namespace Pacer.Steps;

/// <summary>
/// The outcome of a single step execution. The framework measures latency itself; the step only
/// reports whether it succeeded, how much data it moved, an optional payload to hand to the next
/// step in the pipeline, and an optional status label for grouping in reports.
/// </summary>
public readonly record struct StepResult
{
    /// <summary>Whether the step succeeded. A failure stops the rest of the virtual user's journey.</summary>
    public bool IsOk { get; private init; }

    /// <summary>Bytes sent by the step (e.g. the request body), used for throughput reporting. Zero if not tracked.</summary>
    public long BytesSent { get; private init; }

    /// <summary>Bytes received by the step (e.g. the response body), used for throughput reporting. Zero if not tracked.</summary>
    public long BytesReceived { get; private init; }

    /// <summary>Total bytes transferred by the step (<see cref="BytesSent"/> + <see cref="BytesReceived"/>).</summary>
    public long TotalBytes => BytesSent + BytesReceived;

    /// <summary>An optional value passed to the next step via <see cref="IStepContext.Previous"/>.</summary>
    public object? Payload { get; private init; }

    /// <summary>An optional label (e.g. an HTTP status) used to group results in reports.</summary>
    public string? Status { get; private init; }

    /// <summary>Creates a successful result.</summary>
    public static StepResult Ok(long bytesSent = 0, long bytesReceived = 0, object? payload = null, string? status = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bytesSent);
        ArgumentOutOfRangeException.ThrowIfNegative(bytesReceived);
        return new StepResult { IsOk = true, BytesSent = bytesSent, BytesReceived = bytesReceived, Payload = payload, Status = status };
    }

    /// <summary>Creates a failed result.</summary>
    public static StepResult Fail(string? status = null)
        => new() { IsOk = false, Status = status };

    /// <summary>
    /// Returns a copy with the byte totals replaced. Used by the engine to fold in bytes accumulated
    /// via the per-step context counters (<see cref="IStepContext.AddBytesSent"/> /
    /// <see cref="IStepContext.AddBytesReceived"/>) alongside any reported on the result itself.
    /// </summary>
    internal StepResult WithBytes(long bytesSent, long bytesReceived)
        => this with { BytesSent = bytesSent, BytesReceived = bytesReceived };
}
