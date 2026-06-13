namespace Pacer.Metrics;

/// <summary>Static information about the machine and runtime a test ran on.</summary>
public sealed record EnvironmentInfo(string OperatingSystem, string RuntimeVersion, int ProcessorCount, string MachineName)
{
    /// <summary>Captures the current environment.</summary>
    public static EnvironmentInfo Capture() => new(
        System.Runtime.InteropServices.RuntimeInformation.OSDescription,
        System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        Environment.ProcessorCount,
        Environment.MachineName);
}

/// <summary>A point-in-time sample of throughput taken at a fixed interval during a run.</summary>
public sealed record IntervalSnapshot(TimeSpan Elapsed, int ActiveUsers, long Ok, long Fail, double RequestsPerSecond);

/// <summary>
/// The complete result of running one scenario: per-step statistics, an aggregate journey row,
/// per-interval throughput, and run/environment metadata. Consumed by the report writers.
/// </summary>
public sealed record RunReport
{
    /// <summary>The scenario that was run.</summary>
    public required string ScenarioName { get; init; }

    /// <summary>The scenario's group, if any.</summary>
    public string? Group { get; init; }

    /// <summary>The kind of load profile used (e.g. "Ramp").</summary>
    public required string LoadProfileKind { get; init; }

    /// <summary>The peak target virtual-user count of the load profile.</summary>
    public int PeakUsers { get; init; }

    /// <summary>When measurement started (after warm-up).</summary>
    public DateTimeOffset StartedAt { get; init; }

    /// <summary>The measured duration (excludes warm-up).</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Per-step statistics in declaration order.</summary>
    public required IReadOnlyList<StepStats> Steps { get; init; }

    /// <summary>Aggregate statistics for the full journey (named after the scenario).</summary>
    public required StepStats Journey { get; init; }

    /// <summary>Per-interval throughput snapshots captured during the run.</summary>
    public IReadOnlyList<IntervalSnapshot> Intervals { get; init; } = [];

    /// <summary>The machine and runtime the test ran on.</summary>
    public required EnvironmentInfo Environment { get; init; }
}
