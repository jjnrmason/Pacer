using Microsoft.Extensions.Logging;

namespace Pacer.Scenarios;

/// <summary>The context handed to a scenario's one-time setup and teardown.</summary>
public interface ISetupContext
{
    /// <summary>The service provider for resolving dependencies during setup.</summary>
    IServiceProvider Services { get; }

    /// <summary>A logger for the running scenario.</summary>
    ILogger Logger { get; }

    /// <summary>The name of the scenario being set up.</summary>
    string ScenarioName { get; }
}

/// <summary>
/// A scenario's one-time setup, defined inline on the scenario. It runs once before the load phase
/// and any value it returns is exposed to every step via
/// <see cref="Steps.IStepContext.ScenarioData"/> (e.g. an auth token or seeded test data).
/// </summary>
public delegate ValueTask<object?> ScenarioSetupDelegate(ISetupContext context, CancellationToken cancellationToken);

/// <summary>A scenario's one-time teardown, run once after the load phase completes.</summary>
public delegate ValueTask ScenarioTeardownDelegate(ISetupContext context, CancellationToken cancellationToken);
