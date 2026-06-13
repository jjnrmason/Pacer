namespace Pacer.Metrics;

/// <summary>
/// Thread-safe accumulator of one step's results. To keep the hot recording path cheap under many
/// concurrent virtual users, samples are written into per-CPU shards (each with its own histogram
/// and lock); the shards are merged only when a <see cref="StepStats"/> snapshot is taken.
/// </summary>
public sealed class StepStatsCollector
{
    private readonly Shard[] _shards;

    /// <summary>The name of the step (or scenario, for the journey aggregate) being measured.</summary>
    public string Name { get; }

    /// <summary>Creates a collector for the named step.</summary>
    public StepStatsCollector(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;

        var shardCount = Math.Max(1, Environment.ProcessorCount);
        _shards = new Shard[shardCount];
        for (var i = 0; i < shardCount; i++)
            _shards[i] = new Shard();
    }

    /// <summary>Records a single execution result.</summary>
    public void Record(TimeSpan latency, bool isOk, long sizeBytes = 0)
    {
        var shard = _shards[(uint)Thread.GetCurrentProcessorId() % (uint)_shards.Length];
        lock (shard.Gate)
        {
            shard.Histogram.Record(latency);
            if (isOk)
                shard.Ok++;
            else
                shard.Fail++;
            shard.Bytes += sizeBytes;
        }
    }

    /// <summary>
    /// Merges all shards and produces an immutable snapshot. <paramref name="measuredWindow"/> is the
    /// duration over which throughput is computed.
    /// </summary>
    public StepStats Snapshot(TimeSpan measuredWindow)
    {
        var merged = new LatencyHistogram();
        long ok = 0, fail = 0, bytes = 0;

        foreach (var shard in _shards)
        {
            lock (shard.Gate)
            {
                merged.Add(shard.Histogram);
                ok += shard.Ok;
                fail += shard.Fail;
                bytes += shard.Bytes;
            }
        }

        var seconds = measuredWindow.TotalSeconds;
        var rps = seconds > 0 ? (ok + fail) / seconds : 0;

        return new StepStats
        {
            Name = Name,
            Ok = ok,
            Fail = fail,
            TotalBytes = bytes,
            RequestsPerSecond = rps,
            MinMs = ToMs(merged.MinMicroseconds),
            MeanMs = merged.MeanMicroseconds / 1000d,
            MaxMs = ToMs(merged.MaxMicroseconds),
            P50Ms = ToMs(merged.ValueAtPercentileMicroseconds(50)),
            P75Ms = ToMs(merged.ValueAtPercentileMicroseconds(75)),
            P95Ms = ToMs(merged.ValueAtPercentileMicroseconds(95)),
            P99Ms = ToMs(merged.ValueAtPercentileMicroseconds(99)),
            P999Ms = ToMs(merged.ValueAtPercentileMicroseconds(99.9)),
        };
    }

    private static double ToMs(long microseconds) => microseconds / 1000d;

    private sealed class Shard
    {
        public readonly object Gate = new();
        public readonly LatencyHistogram Histogram = new();
        public long Ok;
        public long Fail;
        public long Bytes;
    }
}
