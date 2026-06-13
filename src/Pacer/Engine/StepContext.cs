using Microsoft.Extensions.Logging;
using Pacer.Steps;

namespace Pacer.Engine;

/// <summary>
/// The mutable per-virtual-user implementation of <see cref="IStepContext"/>. The framework reuses
/// a single instance per virtual user across journeys, updating the per-journey fields each iteration.
/// </summary>
internal sealed class StepContext : IStepContext
{
    public StepContext(object? scenarioData, int virtualUserId, Random random, ILogger logger, IServiceProvider services)
    {
        ScenarioData = scenarioData;
        VirtualUserId = virtualUserId;
        Random = random;
        Logger = logger;
        Services = services;
    }

    public object? ScenarioData { get; }

    public object? Previous { get; set; }

    public long InvocationNumber { get; set; }

    public int VirtualUserId { get; }

    public Random Random { get; }

    public IServiceProvider Services { get; set; }

    public ILogger Logger { get; }

    public CancellationToken CancellationToken { get; set; }
}
