using Pacer.Metrics;
using Pacer.Scenarios;

namespace Pacer.Engine;

/// <summary>
/// Orchestrates running one scenario, a named group, or every registered scenario against the
/// <see cref="ScenarioRegistry"/>, returning a <see cref="RunReport"/> for each. Scenarios run
/// sequentially so they do not compete for the same machine resources.
/// </summary>
public sealed class TestRunner
{
    private readonly ScenarioRegistry _registry;
    private readonly ScenarioRunner _runner;

    /// <summary>Creates a test runner.</summary>
    public TestRunner(ScenarioRegistry registry, ScenarioRunner runner)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(runner);
        _registry = registry;
        _runner = runner;
    }

    /// <summary>
    /// Runs the single named scenario. An optional <paramref name="transform"/> can adjust the
    /// definition before it runs (used to apply command-line overrides).
    /// </summary>
    public async Task<IReadOnlyList<RunReport>> RunScenarioAsync(
        string name, Func<ScenarioDefinition, ScenarioDefinition>? transform = null, CancellationToken cancellationToken = default)
    {
        var scenario = Transform(_registry.Get(name), transform);
        return [await _runner.RunAsync(scenario, cancellationToken).ConfigureAwait(false)];
    }

    /// <summary>Runs every scenario in the named group.</summary>
    public Task<IReadOnlyList<RunReport>> RunGroupAsync(
        string group, Func<ScenarioDefinition, ScenarioDefinition>? transform = null, CancellationToken cancellationToken = default)
    {
        var scenarios = _registry.InGroup(group).ToArray();
        if (scenarios.Length == 0)
            throw new KeyNotFoundException($"No scenarios are registered in group '{group}'.");
        return RunManyAsync(scenarios, transform, cancellationToken);
    }

    /// <summary>Runs every registered scenario.</summary>
    public Task<IReadOnlyList<RunReport>> RunAllAsync(
        Func<ScenarioDefinition, ScenarioDefinition>? transform = null, CancellationToken cancellationToken = default)
    {
        var scenarios = _registry.All.ToArray();
        if (scenarios.Length == 0)
            throw new InvalidOperationException("No scenarios are registered.");
        return RunManyAsync(scenarios, transform, cancellationToken);
    }

    private async Task<IReadOnlyList<RunReport>> RunManyAsync(
        IReadOnlyList<ScenarioDefinition> scenarios, Func<ScenarioDefinition, ScenarioDefinition>? transform, CancellationToken cancellationToken)
    {
        var reports = new List<RunReport>(scenarios.Count);
        foreach (var scenario in scenarios)
        {
            cancellationToken.ThrowIfCancellationRequested();
            reports.Add(await _runner.RunAsync(Transform(scenario, transform), cancellationToken).ConfigureAwait(false));
        }

        return reports;
    }

    private static ScenarioDefinition Transform(ScenarioDefinition scenario, Func<ScenarioDefinition, ScenarioDefinition>? transform)
        => transform is null ? scenario : transform(scenario);
}
