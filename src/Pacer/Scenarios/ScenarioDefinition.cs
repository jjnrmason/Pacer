using Pacer.Load;
using Pacer.Steps;

namespace Pacer.Scenarios;

/// <summary>
/// An immutable, validated description of a scenario: its steps, optional grouping and one-time
/// setup/teardown, warm-up duration, and load profile. Produced by <see cref="Scenario.Build"/> and
/// the unit the engine executes.
/// </summary>
public sealed class ScenarioDefinition
{
    /// <summary>The scenario's unique name.</summary>
    public string Name { get; }

    /// <summary>An optional group the scenario belongs to, enabling group runs. May be <see langword="null"/>.</summary>
    public string? Group { get; }

    /// <summary>The steps executed, in order, as a pipeline for each virtual user.</summary>
    public IReadOnlyList<Step> Steps { get; }

    /// <summary>The inline one-time setup that produces the scenario data, or <see langword="null"/>.</summary>
    public ScenarioSetupDelegate? Setup { get; }

    /// <summary>The inline one-time teardown, or <see langword="null"/>.</summary>
    public ScenarioTeardownDelegate? Teardown { get; }

    /// <summary>How long to run un-recorded warm-up journeys before measurement begins.</summary>
    public TimeSpan Warmup { get; }

    /// <summary>The load profile that drives virtual-user concurrency over time.</summary>
    public LoadProfile Load { get; }

    internal ScenarioDefinition(
        string name,
        string? group,
        IReadOnlyList<Step> steps,
        ScenarioSetupDelegate? setup,
        ScenarioTeardownDelegate? teardown,
        TimeSpan warmup,
        LoadProfile load)
    {
        Name = name;
        Group = group;
        Steps = steps;
        Setup = setup;
        Teardown = teardown;
        Warmup = warmup;
        Load = load;
    }

    /// <summary>Returns a copy of this definition with a different load profile (used for CLI overrides).</summary>
    public ScenarioDefinition WithLoad(LoadProfile load)
    {
        ArgumentNullException.ThrowIfNull(load);
        return new ScenarioDefinition(Name, Group, Steps, Setup, Teardown, Warmup, load);
    }

    /// <summary>Returns a copy of this definition with a different warm-up duration (used for CLI overrides).</summary>
    public ScenarioDefinition WithWarmup(TimeSpan warmup)
    {
        if (warmup < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(warmup), warmup, "Warm-up duration cannot be negative.");
        return new ScenarioDefinition(Name, Group, Steps, Setup, Teardown, warmup, Load);
    }
}
