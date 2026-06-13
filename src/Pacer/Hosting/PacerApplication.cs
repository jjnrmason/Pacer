using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pacer.Engine;
using Pacer.Metrics;
using Pacer.Reporting;
using Pacer.Scenarios;

namespace Pacer.Hosting;

/// <summary>
/// The entry point for a Pacer console application. Builds a Generic Host (dependency injection,
/// configuration, console logging), wires the command-line surface, and dispatches <c>run</c> and
/// <c>list</c> commands. A consumer's <c>Program</c> typically calls
/// <see cref="RunAsync(string[], Action{HostApplicationBuilder}, CancellationToken)"/> and registers
/// scenarios via <see cref="PacerServiceCollectionExtensions.AddPacer"/>.
/// </summary>
public static class PacerApplication
{
    /// <summary>Builds the host, applies <paramref name="configure"/>, and runs the requested command.</summary>
    /// <example>
    /// A complete <c>Program.cs</c> — register scenarios, then dispatch the command-line arguments:
    /// <code><![CDATA[
    /// return await PacerApplication.RunAsync(args, builder =>
    /// {
    ///     builder.Services.AddPacer()
    ///         .AddScenario(SampleScenarios.Checkout());
    /// });
    ///
    /// // dotnet run -- list
    /// // dotnet run -- run --scenario checkout --out ./reports
    /// // dotnet run -- run --group storefront --profile stress --users 100 --duration 5
    /// ]]></code>
    /// </example>
    public static async Task<int> RunAsync(string[] args, Action<HostApplicationBuilder> configure, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = Host.CreateApplicationBuilder(args);
        builder.Logging.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        });

        configure(builder);

        using var host = builder.Build();

        var cli = new PacerCommandLine(
            (options, ct) => RunCommandAsync(host.Services, options, ct),
            ct => ListCommandAsync(host.Services, ct));

        return await cli.Root.Parse(args).InvokeAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static async Task<int> RunCommandAsync(IServiceProvider services, RunOptions options, CancellationToken cancellationToken)
    {
        var runner = services.GetRequiredService<TestRunner>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Pacer");

        ScenarioDefinition Transform(ScenarioDefinition scenario) => RunPlanner.Apply(scenario, options);

        IReadOnlyList<RunReport> reports;
        try
        {
            if (options.All)
                reports = await runner.RunAllAsync(Transform, cancellationToken).ConfigureAwait(false);
            else if (options.Group is not null)
                reports = await runner.RunGroupAsync(options.Group, Transform, cancellationToken).ConfigureAwait(false);
            else if (options.Scenario is not null)
                reports = await runner.RunScenarioAsync(options.Scenario, Transform, cancellationToken).ConfigureAwait(false);
            else
            {
                logger.LogError("Specify --scenario <name>, --group <name>, or --all.");
                return 1;
            }
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogError("{Message}", ex.Message);
            return 1;
        }

        // Every registered report writer (the built-in console/CSV/HTML plus any custom ones) runs.
        foreach (var writer in services.GetServices<IReportWriter>())
            await writer.WriteAsync(reports, options.OutputDirectory, cancellationToken).ConfigureAwait(false);

        return 0;
    }

    private static Task<int> ListCommandAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var registry = services.GetRequiredService<ScenarioRegistry>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Pacer");

        var scenarios = registry.All.OrderBy(s => s.Name).ToArray();
        if (scenarios.Length == 0)
        {
            logger.LogWarning("No scenarios are registered.");
            return Task.FromResult(0);
        }

        foreach (var scenario in scenarios)
        {
            var group = scenario.Group is null ? "" : $" [group: {scenario.Group}]";
            logger.LogInformation("{Scenario}{Group} — {Steps} steps, {Profile} profile", scenario.Name, group, scenario.Steps.Count, scenario.Load.Kind);
        }

        return Task.FromResult(0);
    }
}
