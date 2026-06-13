using Pacer.Metrics;

namespace Pacer.Tests.Metrics.WhenUsingTheLatencyHistogram;

public class LatencyHistogramTestBase
{
    protected LatencyHistogram LatencyHistogram { get; private set; } = null!;

    [SetUp]
    public void SetUp()
    {
        this.LatencyHistogram = new LatencyHistogram();
    }

    protected void RecordRange(long fromMicroseconds, long toInclusiveMicroseconds, long stepMicroseconds)
    {
        for (var v = fromMicroseconds; v <= toInclusiveMicroseconds; v += stepMicroseconds)
        {
            this.LatencyHistogram.Record(v);
        }
    }
}