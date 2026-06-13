namespace Pacer.Tests.Metrics.WhenCollectingStepStats;

public partial class WhenCollectingStepStats
{
    public class AndTakingASnapshot : StepStatsCollectorTestBase
    {
        [Test]
        public void ThenItCountsSuccesses()
        {
            this.StepStatsCollector.Record(TimeSpan.FromMilliseconds(10), isOk: true);
            this.StepStatsCollector.Record(TimeSpan.FromMilliseconds(10), isOk: true);

            var snapshot = this.StepStatsCollector.Snapshot(TimeSpan.FromSeconds(1));

            Assert.That(snapshot.Ok, Is.EqualTo(2));
        }

        [Test]
        public void ThenItCountsFailures()
        {
            this.StepStatsCollector.Record(TimeSpan.FromMilliseconds(10), isOk: true);
            this.StepStatsCollector.Record(TimeSpan.FromMilliseconds(10), isOk: false);

            var snapshot = this.StepStatsCollector.Snapshot(TimeSpan.FromSeconds(1));

            Assert.That(snapshot.Fail, Is.EqualTo(1));
        }

        [Test]
        public void ThenItSumsTransferredBytes()
        {
            this.StepStatsCollector.Record(TimeSpan.FromMilliseconds(10), isOk: true, sizeBytes: 100);
            this.StepStatsCollector.Record(TimeSpan.FromMilliseconds(10), isOk: true, sizeBytes: 250);

            var snapshot = this.StepStatsCollector.Snapshot(TimeSpan.FromSeconds(1));

            Assert.That(snapshot.TotalBytes, Is.EqualTo(350));
        }

        [Test]
        public void ThenItComputesThroughputOverTheWindow()
        {
            for (var i = 0; i < 100; i++)
            {
                this.StepStatsCollector.Record(TimeSpan.FromMilliseconds(5), isOk: true);
            }

            var snapshot = this.StepStatsCollector.Snapshot(TimeSpan.FromSeconds(10));

            Assert.That(snapshot.RequestsPerSecond, Is.EqualTo(10));
        }

        [Test]
        public void ThenItReportsLatencyInMilliseconds()
        {
            this.StepStatsCollector.Record(TimeSpan.FromMilliseconds(20), isOk: true);

            var snapshot = this.StepStatsCollector.Snapshot(TimeSpan.FromSeconds(1));

            Assert.That(snapshot.P50Ms, Is.EqualTo(20).Within(5).Percent);
        }

        [Test]
        public void ThenItCarriesTheStepName()
        {
            var snapshot = this.StepStatsCollector.Snapshot(TimeSpan.FromSeconds(1));

            Assert.That(snapshot.Name, Is.EqualTo("login"));
        }
    }
}