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

    /// <summary>Bytes transferred by the step, used for throughput reporting. Zero if not tracked.</summary>
    public long SizeBytes { get; private init; }

    /// <summary>An optional value passed to the next step via <see cref="IStepContext.Previous"/>.</summary>
    public object? Payload { get; private init; }

    /// <summary>An optional label (e.g. an HTTP status) used to group results in reports.</summary>
    public string? Status { get; private init; }

    /// <summary>Creates a successful result.</summary>
    public static StepResult Ok(long sizeBytes = 0, object? payload = null, string? status = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sizeBytes);
        return new StepResult { IsOk = true, SizeBytes = sizeBytes, Payload = payload, Status = status };
    }

    /// <summary>Creates a failed result.</summary>
    public static StepResult Fail(string? status = null)
        => new() { IsOk = false, Status = status };
}
