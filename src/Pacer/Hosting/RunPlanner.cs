using Pacer.Load;
using Pacer.Scenarios;

namespace Pacer.Hosting;

/// <summary>
/// Applies command-line <see cref="RunOptions"/> overrides to a scenario, rebuilding its load
/// profile (and warm-up) from the requested shape, user count, and duration.
/// </summary>
public static class RunPlanner
{
    /// <summary>Returns the scenario unchanged, or a copy with overrides applied.</summary>
    public static ScenarioDefinition Apply(ScenarioDefinition scenario, RunOptions options)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(options);

        var result = scenario;
        if (options.OverridesLoad)
            result = result.WithLoad(BuildProfile(options, scenario.Load));
        if (options.Warmup is { } warmup)
            result = result.WithWarmup(warmup);
        return result;
    }

    /// <summary>
    /// Builds a load profile from the overrides, defaulting the user count and duration to the
    /// existing profile's peak and total when not specified, and the shape to "load".
    /// </summary>
    public static LoadProfile BuildProfile(RunOptions options, LoadProfile fallback)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(fallback);

        var users = options.Users ?? fallback.PeakUsers;
        var duration = options.Duration ?? fallback.TotalDuration;
        var shape = (options.Profile ?? "load").ToLowerInvariant();

        return shape switch
        {
            "load" => LoadProfiles.Load(users, duration),
            "soak" => LoadProfiles.Soak(users, duration),
            "spike" => LoadProfiles.Spike(Math.Max(1, users / 4), users, duration / 5, duration),
            "stress" => LoadProfiles.Stress(Math.Max(1, users / 5), users, Math.Max(1, users / 5), duration / 5),
            "ramp" => LoadProfiles.Ramp(users, duration / 3, duration / 3, duration - 2 * (duration / 3)),
            _ => throw new ArgumentException($"Unknown load profile '{options.Profile}'. Valid values: load, soak, spike, stress, ramp.", nameof(options)),
        };
    }
}
