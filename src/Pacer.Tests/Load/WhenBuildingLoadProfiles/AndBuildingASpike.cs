using Pacer.Load;

namespace Pacer.Tests.Load.WhenBuildingLoadProfiles;

public partial class WhenBuildingLoadProfiles
{
    public class AndBuildingASpike
    {
        [Test]
        public void ThenItProducesBaselineSpikeBaselineStages()
        {
            var profile = LoadProfiles.Spike(baseline: 10, spike: 200, spikeDuration: TimeSpan.FromSeconds(10), totalDuration: TimeSpan.FromSeconds(30));

            var levels = profile.Stages.Select(s => s.StartUsers).ToArray();

            Assert.That(levels, Is.EqualTo(new[] { 10, 200, 10 }));
        }

        [Test]
        public void ThenItSplitsTheNonSpikeTimeEvenly()
        {
            var profile = LoadProfiles.Spike(baseline: 10, spike: 200, spikeDuration: TimeSpan.FromSeconds(10), totalDuration: TimeSpan.FromSeconds(30));

            var durations = profile.Stages.Select(s => s.Duration).ToArray();

            Assert.That(durations, Is.EqualTo(new[] { TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10) }));
        }

        [Test]
        public void ThenItJumpsToTheSpikeLevelInstantaneously()
        {
            var profile = LoadProfiles.Spike(baseline: 10, spike: 200, spikeDuration: TimeSpan.FromSeconds(10), totalDuration: TimeSpan.FromSeconds(30));

            var result = profile.TargetUsersAt(TimeSpan.FromSeconds(10));

            Assert.That(result, Is.EqualTo(200));
        }

        [Test]
        public void ThenItThrowsWhenTheSpikeIsNotShorterThanTheTotal()
        {
            Assert.That(
                () => LoadProfiles.Spike(baseline: 10, spike: 200, spikeDuration: TimeSpan.FromSeconds(30), totalDuration: TimeSpan.FromSeconds(30)),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}