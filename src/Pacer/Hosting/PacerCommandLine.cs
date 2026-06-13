using System.CommandLine;

namespace Pacer.Hosting;

/// <summary>
/// Builds the Pacer command-line surface (the <c>run</c> and <c>list</c> commands) and binds parsed
/// arguments into a <see cref="RunOptions"/>. Kept internal and separate from execution so the
/// parsing and binding can be unit-tested without running a load test.
/// </summary>
internal sealed class PacerCommandLine
{
    private readonly Option<string?> _scenario = new("--scenario", "-s") { Description = "Name of a single scenario to run." };
    private readonly Option<string?> _group = new("--group", "-g") { Description = "Run every scenario in this group." };
    private readonly Option<bool> _all = new("--all") { Description = "Run every registered scenario." };
    private readonly Option<int> _users = new("--users", "-u") { Description = "Override the peak number of virtual users." };
    private readonly Option<int> _duration = new("--duration", "-d") { Description = "Override the test duration, in minutes." };
    private readonly Option<string?> _profile = new("--profile", "-p") { Description = "Override the load profile: load, soak, spike, stress, or ramp." };
    private readonly Option<int> _warmup = new("--warmup", "-w") { Description = "Override the warm-up duration, in minutes." };
    private readonly Option<string?> _out = new("--out", "-o") { Description = "Output directory for file-based reports." };

    /// <summary>The configured root command.</summary>
    public RootCommand Root { get; }

    public PacerCommandLine(
        Func<RunOptions, CancellationToken, Task<int>> runHandler,
        Func<CancellationToken, Task<int>> listHandler)
    {
        ArgumentNullException.ThrowIfNull(runHandler);
        ArgumentNullException.ThrowIfNull(listHandler);

        var run = new Command("run", "Run one or more load-test scenarios.")
        {
            _scenario, _group, _all, _users, _duration, _profile, _warmup, _out,
        };
        run.SetAction((parseResult, cancellationToken) => runHandler(BindRun(parseResult), cancellationToken));

        var list = new Command("list", "List the registered scenarios and groups.");
        list.SetAction((_, cancellationToken) => listHandler(cancellationToken));

        Root = new RootCommand("Pacer — a .NET performance and load testing framework.")
        {
            run, list,
        };
    }

    /// <summary>Maps a parsed result for the <c>run</c> command into <see cref="RunOptions"/>.</summary>
    public RunOptions BindRun(ParseResult parseResult)
    {
        ArgumentNullException.ThrowIfNull(parseResult);

        var users = parseResult.GetValue(_users);
        return new RunOptions
        {
            Scenario = NullIfBlank(parseResult.GetValue(_scenario)),
            Group = NullIfBlank(parseResult.GetValue(_group)),
            All = parseResult.GetValue(_all),
            Users = users > 0 ? users : null,
            Duration = Minutes(parseResult.GetValue(_duration)),
            Profile = NullIfBlank(parseResult.GetValue(_profile)),
            Warmup = Minutes(parseResult.GetValue(_warmup)),
            OutputDirectory = NullIfBlank(parseResult.GetValue(_out)) ?? "reports",
        };
    }

    private static TimeSpan? Minutes(int minutes) => minutes > 0 ? TimeSpan.FromMinutes(minutes) : null;

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
