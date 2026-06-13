using Pacer.Metrics;
using Pacer.Steps;

namespace Pacer.Engine;

/// <summary>
/// The recorder used during the measured phase: it feeds each step result into the per-step
/// <see cref="StepStatsCollector"/> for end-of-run reporting and emits it to the live
/// <see cref="PacerMeter"/>. It also keeps running totals used for per-interval throughput.
/// </summary>
internal sealed class CollectorRecorder : IStepRecorder
{
    private readonly IReadOnlyDictionary<string, StepStatsCollector> _collectors;
    private readonly PacerMeter _meter;
    private readonly string _scenario;
    private long _ok;
    private long _fail;

    public CollectorRecorder(IReadOnlyDictionary<string, StepStatsCollector> collectors, PacerMeter meter, string scenario)
    {
        _collectors = collectors;
        _meter = meter;
        _scenario = scenario;
    }

    /// <summary>Total successful step executions recorded so far.</summary>
    public long Ok => Interlocked.Read(ref _ok);

    /// <summary>Total failed step executions recorded so far.</summary>
    public long Fail => Interlocked.Read(ref _fail);

    public void Record(string stepName, TimeSpan latency, StepResult result)
    {
        if (_collectors.TryGetValue(stepName, out var collector))
            collector.Record(latency, result.IsOk, result.BytesSent, result.BytesReceived);

        if (result.IsOk)
            Interlocked.Increment(ref _ok);
        else
            Interlocked.Increment(ref _fail);

        _meter.RecordRequest(_scenario, stepName, result.IsOk, latency.TotalMilliseconds);
    }
}
