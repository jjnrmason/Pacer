using Pacer.Load;

namespace Pacer.Tests.Load.WhenBuildingLoadProfiles;

public partial class WhenBuildingLoadProfiles
{
    public class AndBuildingARamp
    {
        [Test]
        public void ThenItIncludesAHoldStageWhenHoldIsPositive()
        {
            var profile = LoadProfiles.Ramp(peak: 100, rampUp: TimeSpan.FromSeconds(10), hold: TimeSpan.FromSeconds(30), rampDown: TimeSpan.FromSeconds(10));

            Assert.That(profile.Stages, Has.Count.EqualTo(3));
        }

        [Test]
        public void ThenItOmitsTheHoldStageWhenHoldIsZero()
        {
            var profile = LoadProfiles.Ramp(peak: 100, rampUp: TimeSpan.FromSeconds(10), hold: TimeSpan.Zero, rampDown: TimeSpan.FromSeconds(10));

            Assert.That(profile.Stages, Has.Count.EqualTo(2));
        }

        [Test]
        public void ThenItHoldsAtThePeakThroughTheHoldStage()
        {
            var profile = LoadProfiles.Ramp(peak: 100, rampUp: TimeSpan.FromSeconds(10), hold: TimeSpan.FromSeconds(30), rampDown: TimeSpan.FromSeconds(10));

            var result = profile.TargetUsersAt(TimeSpan.FromSeconds(25));

            Assert.That(result, Is.EqualTo(100));
        }

        [Test]
        public void ThenTheTotalDurationIsTheSumOfAllStages()
        {
            var profile = LoadProfiles.Ramp(peak: 100, rampUp: TimeSpan.FromSeconds(10), hold: TimeSpan.FromSeconds(30), rampDown: TimeSpan.FromSeconds(10));

            Assert.That(profile.TotalDuration, Is.EqualTo(TimeSpan.FromSeconds(50)));
        }
    }
}