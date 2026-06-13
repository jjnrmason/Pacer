using Pacer.Load;

namespace Pacer.Tests.Load.WhenBuildingLoadProfiles;

public partial class WhenBuildingLoadProfiles
{
    public class AndBuildingAStressStaircase
    {
        [Test]
        public void ThenItStepsUpToTheMaximumInclusive()
        {
            var profile = LoadProfiles.Stress(start: 10, max: 40, step: 10, stepDuration: TimeSpan.FromSeconds(5));

            var levels = profile.Stages.Select(s => s.StartUsers).ToArray();

            Assert.That(levels, Is.EqualTo(new[] { 10, 20, 30, 40 }));
        }

        [Test]
        public void ThenItIncludesTheMaximumWhenTheStepDoesNotDivideEvenly()
        {
            var profile = LoadProfiles.Stress(start: 10, max: 35, step: 10, stepDuration: TimeSpan.FromSeconds(5));

            var levels = profile.Stages.Select(s => s.StartUsers).ToArray();

            Assert.That(levels, Is.EqualTo(new[] { 10, 20, 30, 35 }));
        }

        [Test]
        public void ThenItReportsThePeakAsTheMaximum()
        {
            var profile = LoadProfiles.Stress(start: 10, max: 40, step: 10, stepDuration: TimeSpan.FromSeconds(5));

            Assert.That(profile.PeakUsers, Is.EqualTo(40));
        }

        [Test]
        public void ThenItThrowsWhenTheMaximumIsBelowTheStart()
        {
            Assert.That(
                () => LoadProfiles.Stress(start: 50, max: 40, step: 10, stepDuration: TimeSpan.FromSeconds(5)),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}