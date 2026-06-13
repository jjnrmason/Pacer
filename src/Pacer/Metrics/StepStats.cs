namespace Pacer.Metrics;

/// <summary>
/// An immutable snapshot of a single step's (or the whole journey's) measured performance over a
/// run. Latency values are in milliseconds.
/// </summary>
public sealed record StepStats
{
    /// <summary>The step name, or the scenario name for the aggregate journey row.</summary>
    public required string Name { get; init; }

    /// <summary>Count of successful executions.</summary>
    public long Ok { get; init; }

    /// <summary>Count of failed executions.</summary>
    public long Fail { get; init; }

    /// <summary>Total bytes sent across all executions.</summary>
    public long BytesSent { get; init; }

    /// <summary>Total bytes received across all executions.</summary>
    public long BytesReceived { get; init; }

    /// <summary>Total bytes transferred across all executions (<see cref="BytesSent"/> + <see cref="BytesReceived"/>).</summary>
    public long TotalBytes => BytesSent + BytesReceived;

    /// <summary>Throughput in successful-or-failed executions per second over the measured window.</summary>
    public double RequestsPerSecond { get; init; }

    /// <summary>Minimum latency in milliseconds.</summary>
    public double MinMs { get; init; }

    /// <summary>Mean latency in milliseconds.</summary>
    public double MeanMs { get; init; }

    /// <summary>Maximum latency in milliseconds.</summary>
    public double MaxMs { get; init; }

    /// <summary>50th percentile (median) latency in milliseconds.</summary>
    public double P50Ms { get; init; }

    /// <summary>75th percentile latency in milliseconds.</summary>
    public double P75Ms { get; init; }

    /// <summary>95th percentile latency in milliseconds.</summary>
    public double P95Ms { get; init; }

    /// <summary>99th percentile latency in milliseconds.</summary>
    public double P99Ms { get; init; }

    /// <summary>99.9th percentile latency in milliseconds.</summary>
    public double P999Ms { get; init; }

    /// <summary>Total executions (<see cref="Ok"/> + <see cref="Fail"/>).</summary>
    public long Total => Ok + Fail;
}
