using Microsoft.Extensions.Logging;

namespace Pacer.Steps;

/// <summary>
/// The context handed to every step execution. It exposes the data produced by the scenario's
/// one-time setup, the previous step's payload (enabling a data pipeline through the journey),
/// a per-virtual-user random source, and the scoped service provider and logger for the journey.
/// </summary>
public interface IStepContext
{
    /// <summary>The value returned by the scenario's <see cref="Pacer.Scenarios.ScenarioSetupDelegate"/>, if any.</summary>
    object? ScenarioData { get; }

    /// <summary>The payload returned by the previous step in this journey, or <see langword="null"/> for the first step.</summary>
    object? Previous { get; }

    /// <summary>The 1-based number of the current journey iteration for this virtual user.</summary>
    long InvocationNumber { get; }

    /// <summary>A stable identifier for the virtual user executing the step.</summary>
    int VirtualUserId { get; }

    /// <summary>A random source seeded per virtual user for reproducible data selection.</summary>
    Random Random { get; }

    /// <summary>The dependency-injection scope for the current journey.</summary>
    IServiceProvider Services { get; }

    /// <summary>A logger for the running scenario.</summary>
    ILogger Logger { get; }

    /// <summary>Signals that the test is stopping and in-flight work should unwind.</summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Adds to the bytes-sent counter for the current step (e.g. a request body size). The total is
    /// folded into the step's reported transfer — even if the step subsequently fails — so it can be
    /// called as I/O happens without threading the count through the returned <see cref="StepResult"/>.
    /// </summary>
    void AddBytesSent(long bytes);

    /// <summary>
    /// Adds to the bytes-received counter for the current step (e.g. a response body size). See
    /// <see cref="AddBytesSent"/> for how the total is recorded.
    /// </summary>
    void AddBytesReceived(long bytes);
}

/// <summary>Convenience accessors for the loosely-typed values on <see cref="IStepContext"/>.</summary>
public static class StepContextExtensions
{
    /// <summary>Returns the scenario setup data cast to <typeparamref name="T"/>, or <see langword="default"/>.</summary>
    public static T? ScenarioDataAs<T>(this IStepContext context)
        => context.ScenarioData is T value ? value : default;

    /// <summary>Returns the previous step's payload cast to <typeparamref name="T"/>, or <see langword="default"/>.</summary>
    public static T? PreviousAs<T>(this IStepContext context)
        => context.Previous is T value ? value : default;
}
