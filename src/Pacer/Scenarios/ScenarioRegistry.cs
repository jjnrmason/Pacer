namespace Pacer.Scenarios;

/// <summary>
/// Holds the scenarios registered with the application and provides lookups by name, by group, and
/// across all scenarios. Registered as a singleton by <c>AddPacer</c>.
/// </summary>
public sealed class ScenarioRegistry
{
    private readonly Dictionary<string, ScenarioDefinition> _byName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Adds a scenario. Throws if another scenario with the same name is already registered.</summary>
    public void Add(ScenarioDefinition scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        if (!_byName.TryAdd(scenario.Name, scenario))
            throw new InvalidOperationException($"A scenario named '{scenario.Name}' is already registered.");
    }

    /// <summary>Builds and adds a scenario from a fluent builder.</summary>
    public void Add(Scenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        Add(scenario.Build());
    }

    /// <summary>Returns the named scenario, throwing if it is not registered.</summary>
    public ScenarioDefinition Get(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _byName.TryGetValue(name, out var scenario)
            ? scenario
            : throw new KeyNotFoundException($"No scenario named '{name}' is registered.");
    }

    /// <summary>Attempts to find the named scenario.</summary>
    public bool TryGet(string name, out ScenarioDefinition scenario)
        => _byName.TryGetValue(name, out scenario!);

    /// <summary>All registered scenarios.</summary>
    public IReadOnlyCollection<ScenarioDefinition> All => _byName.Values;

    /// <summary>All scenarios assigned to the given group.</summary>
    public IEnumerable<ScenarioDefinition> InGroup(string group)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(group);
        return _byName.Values.Where(s => string.Equals(s.Group, group, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The distinct group names across all registered scenarios.</summary>
    public IReadOnlyCollection<string> Groups
        => _byName.Values
            .Where(s => s.Group is not null)
            .Select(s => s.Group!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
