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

    // Per-step byte counters. The context is reused by a single virtual user and a journey runs its
    // steps sequentially, so these need no synchronisation. The engine drains them after each step.
    private long _bytesSent;
    private long _bytesReceived;

    public void AddBytesSent(long bytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bytes);
        _bytesSent += bytes;
    }

    public void AddBytesReceived(long bytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bytes);
        _bytesReceived += bytes;
    }

    /// <summary>Returns the bytes accumulated since the last drain and resets the counters to zero.</summary>
    internal (long Sent, long Received) TakeBytes()
    {
        var taken = (_bytesSent, _bytesReceived);
        _bytesSent = 0;
        _bytesReceived = 0;
        return taken;
    }
}
