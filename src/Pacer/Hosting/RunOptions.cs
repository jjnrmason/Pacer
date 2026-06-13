namespace Pacer.Hosting;

/// <summary>
/// The resolved command-line options for a <c>run</c> invocation. Values left unset fall back to
/// what each scenario defined in code.
/// </summary>
public sealed record RunOptions
{
    /// <summary>The single scenario to run, if any.</summary>
    public string? Scenario { get; init; }

    /// <summary>The scenario group to run, if any.</summary>
    public string? Group { get; init; }

    /// <summary>Whether to run every registered scenario.</summary>
    public bool All { get; init; }

    /// <summary>Override for the peak number of virtual users, or <see langword="null"/> to keep the coded value.</summary>
    public int? Users { get; init; }

    /// <summary>Override for the test duration, or <see langword="null"/> to keep the coded value.</summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>Override for the load profile shape (load, soak, spike, stress, ramp), or <see langword="null"/>.</summary>
    public string? Profile { get; init; }

    /// <summary>Override for the warm-up duration, or <see langword="null"/> to keep the coded value.</summary>
    public TimeSpan? Warmup { get; init; }

    /// <summary>The directory file-based reports are written to.</summary>
    public string OutputDirectory { get; init; } = "reports";

    /// <summary>True when any field that affects the load profile was overridden.</summary>
    public bool OverridesLoad => Profile is not null || Users is not null || Duration is not null;
}
