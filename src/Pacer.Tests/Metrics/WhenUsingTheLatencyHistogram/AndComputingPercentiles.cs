namespace Pacer.Tests.Metrics.WhenUsingTheLatencyHistogram;

public partial class WhenUsingTheLatencyHistogram
{
    public class AndComputingPercentiles : LatencyHistogramTestBase
    {
        [Test]
        public void ThenTheMedianIsNearTheMiddleOfTheRange()
        {
            RecordRange(1_000, 100_000, 1_000);

            var p50 = this.LatencyHistogram.ValueAtPercentileMicroseconds(50);

            Assert.That(p50, Is.EqualTo(50_000).Within(3).Percent);
        }

        [Test]
        public void ThenTheNinetyNinthPercentileIsNearTheTopOfTheRange()
        {
            RecordRange(1_000, 100_000, 1_000);

            var p99 = this.LatencyHistogram.ValueAtPercentileMicroseconds(99);

            Assert.That(p99, Is.EqualTo(99_000).Within(3).Percent);
        }

        [Test]
        public void ThenPercentilesAreMonotonicallyIncreasing()
        {
            RecordRange(1_000, 100_000, 1_000);

            var p50 = this.LatencyHistogram.ValueAtPercentileMicroseconds(50);
            var p95 = this.LatencyHistogram.ValueAtPercentileMicroseconds(95);

            Assert.That(p95, Is.GreaterThan(p50));
        }

        [Test]
        public void ThenTheMedianOfASingleValueIsThatValue()
        {
            this.LatencyHistogram.Record(5_000);

            var p50 = this.LatencyHistogram.ValueAtPercentileMicroseconds(50);

            Assert.That(p50, Is.EqualTo(5_000).Within(3).Percent);
        }

        [Test]
        public void ThenAnEmptyHistogramReportsZero()
        {
            var p50 = this.LatencyHistogram.ValueAtPercentileMicroseconds(50);

            Assert.That(p50, Is.EqualTo(0));
        }
    }
}