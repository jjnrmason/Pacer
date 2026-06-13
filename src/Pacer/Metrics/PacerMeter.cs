using System.Diagnostics.Metrics;

namespace Pacer.Metrics;

/// <summary>
/// The observability layer: a <see cref="Meter"/> named <see cref="MeterName"/> exposing live
/// instruments (request counts, latency, active virtual users) that can be watched with
/// <c>dotnet-counters</c> or <c>dotnet monitor</c> while a test runs. This is separate from the
/// in-process <see cref="StepStatsCollector"/> used to build end-of-run reports.
/// </summary>
public sealed class PacerMeter : IDisposable
{
    /// <summary>The meter name to subscribe to (e.g. with <c>dotnet-counters monitor</c>).</summary>
    public const string MeterName = "Pacer";

    private readonly Meter _meter;
    private readonly Counter<long> _requests;
    private readonly Histogram<double> _latencyMs;
    private readonly UpDownCounter<long> _activeUsers;

    /// <summary>Creates the meter and its instruments.</summary>
    public PacerMeter(IMeterFactory? meterFactory = null)
    {
        _meter = meterFactory?.Create(MeterName) ?? new Meter(MeterName, "1.0.0");
        _requests = _meter.CreateCounter<long>("pacer.requests", unit: "{request}", description: "Number of step executions.");
        _latencyMs = _meter.CreateHistogram<double>("pacer.latency", unit: "ms", description: "Step execution latency.");
        _activeUsers = _meter.CreateUpDownCounter<long>("pacer.active_users", unit: "{user}", description: "Active virtual users.");
    }

    /// <summary>Records a step execution: latency and outcome, tagged by scenario and step.</summary>
    public void RecordRequest(string scenario, string step, bool isOk, double latencyMs)
    {
        var scenarioTag = new KeyValuePair<string, object?>("scenario", scenario);
        var stepTag = new KeyValuePair<string, object?>("step", step);
        var outcomeTag = new KeyValuePair<string, object?>("outcome", isOk ? "ok" : "fail");

        _requests.Add(1, scenarioTag, stepTag, outcomeTag);
        _latencyMs.Record(latencyMs, scenarioTag, stepTag);
    }

    /// <summary>Adjusts the reported number of active virtual users by <paramref name="delta"/>.</summary>
    public void AddActiveUsers(string scenario, long delta)
        => _activeUsers.Add(delta, new KeyValuePair<string, object?>("scenario", scenario));

    /// <summary>Disposes the underlying meter.</summary>
    public void Dispose() => _meter.Dispose();
}
