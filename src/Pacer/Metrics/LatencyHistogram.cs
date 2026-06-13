using System.Numerics;

namespace Pacer.Metrics;

/// <summary>
/// A bounded-memory latency histogram with configurable relative precision, recording values in
/// microseconds. It uses the same sub-bucketed, power-of-two layout as HdrHistogram, giving
/// constant memory and roughly constant relative error across the whole range — so percentiles can
/// be computed without retaining every sample. Not thread-safe; callers serialise access.
/// </summary>
public sealed class LatencyHistogram
{
    private readonly long[] _counts;
    private readonly int _subBucketCount;
    private readonly int _subBucketHalfCount;
    private readonly int _subBucketHalfCountMagnitude;
    private readonly int _leadingZeroCountBase;
    private readonly long _maxValue;

    private long _totalCount;
    private long _min = long.MaxValue;
    private long _max;
    private long _sum;

    /// <summary>
    /// Creates a histogram covering 1 µs up to <paramref name="maxValueMicroseconds"/> (default 600 s)
    /// with <paramref name="significantBits"/> bits of resolution per power-of-two magnitude
    /// (default 6 → ~1.5% relative error).
    /// </summary>
    public LatencyHistogram(long maxValueMicroseconds = 600_000_000L, int significantBits = 6)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(significantBits, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(significantBits, 16);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxValueMicroseconds);

        _maxValue = maxValueMicroseconds;
        _subBucketCount = 1 << significantBits;
        _subBucketHalfCount = _subBucketCount / 2;
        _subBucketHalfCountMagnitude = significantBits - 1;
        _leadingZeroCountBase = 64 - significantBits;

        var bucketCount = BucketsNeededToCover(maxValueMicroseconds);
        _counts = new long[(bucketCount + 1) * _subBucketHalfCount];
    }

    /// <summary>The number of recorded samples.</summary>
    public long Count => _totalCount;

    /// <summary>The smallest recorded value in microseconds, or zero if empty.</summary>
    public long MinMicroseconds => _totalCount == 0 ? 0 : _min;

    /// <summary>The largest recorded value in microseconds.</summary>
    public long MaxMicroseconds => _max;

    /// <summary>The arithmetic mean of recorded values in microseconds, or zero if empty.</summary>
    public double MeanMicroseconds => _totalCount == 0 ? 0 : (double)_sum / _totalCount;

    /// <summary>Records a latency. Values above the configured maximum are clamped.</summary>
    public void Record(TimeSpan latency) => Record((long)latency.TotalMicroseconds);

    /// <summary>Records a latency in microseconds. Negative values are treated as zero; large values are clamped.</summary>
    public void Record(long microseconds)
    {
        if (microseconds < 0)
            microseconds = 0;
        else if (microseconds > _maxValue)
            microseconds = _maxValue;

        _counts[CountsIndexFor(microseconds)]++;
        _totalCount++;
        _sum += microseconds;
        if (microseconds < _min)
            _min = microseconds;
        if (microseconds > _max)
            _max = microseconds;
    }

    /// <summary>Merges another histogram of the same shape into this one.</summary>
    public void Add(LatencyHistogram other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (other._counts.Length != _counts.Length)
            throw new ArgumentException("Histograms must have the same configuration to be merged.", nameof(other));
        if (other._totalCount == 0)
            return;

        for (var i = 0; i < _counts.Length; i++)
            _counts[i] += other._counts[i];

        _totalCount += other._totalCount;
        _sum += other._sum;
        if (other._min < _min)
            _min = other._min;
        if (other._max > _max)
            _max = other._max;
    }

    /// <summary>Returns the value in microseconds at the given percentile (0–100).</summary>
    public long ValueAtPercentileMicroseconds(double percentile)
    {
        if (_totalCount == 0)
            return 0;

        var p = Math.Clamp(percentile, 0d, 100d);
        var target = (long)Math.Ceiling(p / 100d * _totalCount);
        if (target < 1)
            target = 1;

        long running = 0;
        for (var i = 0; i < _counts.Length; i++)
        {
            running += _counts[i];
            if (running >= target)
                return MidValueOfIndex(i);
        }

        return _max;
    }

    private int CountsIndexFor(long value)
    {
        var bucketIndex = BucketIndexOf(value);
        var subBucketIndex = (int)(value >> bucketIndex);
        var bucketBaseIndex = (bucketIndex + 1) << _subBucketHalfCountMagnitude;
        var offsetInBucket = subBucketIndex - _subBucketHalfCount;
        return bucketBaseIndex + offsetInBucket;
    }

    private int BucketIndexOf(long value)
        => _leadingZeroCountBase - BitOperations.LeadingZeroCount((ulong)(value | (long)(_subBucketCount - 1)));

    private long MidValueOfIndex(int index)
    {
        var bucketIndex = (index >> _subBucketHalfCountMagnitude) - 1;
        var subBucketIndex = (index & (_subBucketHalfCount - 1)) + _subBucketHalfCount;
        if (bucketIndex < 0)
        {
            subBucketIndex -= _subBucketHalfCount;
            bucketIndex = 0;
        }

        var lowerBound = (long)subBucketIndex << bucketIndex;
        var rangeSize = 1L << bucketIndex;
        return lowerBound + rangeSize / 2;
    }

    private int BucketsNeededToCover(long value)
    {
        long smallestUntrackable = (long)_subBucketCount;
        var buckets = 1;
        while (smallestUntrackable <= value)
        {
            if (smallestUntrackable > long.MaxValue / 2)
                return buckets + 1;
            smallestUntrackable <<= 1;
            buckets++;
        }

        return buckets;
    }
}
