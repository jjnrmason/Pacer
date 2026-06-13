using Pacer.Metrics;

namespace Pacer.Tests.Metrics.WhenUsingTheLatencyHistogram;

public partial class WhenUsingTheLatencyHistogram
{
    public class AndSummarisingValues : LatencyHistogramTestBase
    {
        [Test]
        public void ThenItTracksTheExactMinimum()
        {
            this.LatencyHistogram.Record(3_000);
            this.LatencyHistogram.Record(1_000);
            this.LatencyHistogram.Record(9_000);

            Assert.That(this.LatencyHistogram.MinMicroseconds, Is.EqualTo(1_000));
        }

        [Test]
        public void ThenItTracksTheExactMaximum()
        {
            this.LatencyHistogram.Record(3_000);
            this.LatencyHistogram.Record(1_000);
            this.LatencyHistogram.Record(9_000);

            Assert.That(this.LatencyHistogram.MaxMicroseconds, Is.EqualTo(9_000));
        }

        [Test]
        public void ThenItTracksTheExactMean()
        {
            this.LatencyHistogram.Record(2_000);
            this.LatencyHistogram.Record(4_000);
            this.LatencyHistogram.Record(6_000);

            Assert.That(this.LatencyHistogram.MeanMicroseconds, Is.EqualTo(4_000));
        }

        [Test]
        public void ThenItCountsEverySample()
        {
            RecordRange(1_000, 10_000, 1_000);

            Assert.That(this.LatencyHistogram.Count, Is.EqualTo(10));
        }

        [Test]
        public void ThenMergingCombinesCountsAndExtremes()
        {
            this.LatencyHistogram.Record(5_000);
            var other = new LatencyHistogram();
            other.Record(1_000);
            other.Record(50_000);

            this.LatencyHistogram.Add(other);

            Assert.That(this.LatencyHistogram.Count, Is.EqualTo(3));
        }

        [Test]
        public void ThenMergingTakesTheLowerMinimum()
        {
            this.LatencyHistogram.Record(5_000);
            var other = new LatencyHistogram();
            other.Record(1_000);

            this.LatencyHistogram.Add(other);

            Assert.That(this.LatencyHistogram.MinMicroseconds, Is.EqualTo(1_000));
        }
    }
}