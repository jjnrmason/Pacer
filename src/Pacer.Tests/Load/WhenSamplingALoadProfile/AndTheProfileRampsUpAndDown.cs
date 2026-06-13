namespace Pacer.Tests.Load.WhenSamplingALoadProfile;

public partial class WhenSamplingALoadProfile
{
    public class AndTheProfileRampsUpAndDown : LoadProfileTestBase
    {
        [Test]
        public void ThenItStartsAtZero()
        {
            var profile = RampProfile(100, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

            var result = profile.TargetUsersAt(TimeSpan.Zero);

            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void ThenItReachesHalfPeakAtTheRampUpMidpoint()
        {
            var profile = RampProfile(100, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

            var result = profile.TargetUsersAt(TimeSpan.FromSeconds(5));

            Assert.That(result, Is.EqualTo(50));
        }

        [Test]
        public void ThenItReachesThePeakAtTheTopOfTheRamp()
        {
            var profile = RampProfile(100, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

            var result = profile.TargetUsersAt(TimeSpan.FromSeconds(10));

            Assert.That(result, Is.EqualTo(100));
        }

        [Test]
        public void ThenItIsSymmetricOnTheWayDown()
        {
            var profile = RampProfile(100, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

            var result = profile.TargetUsersAt(TimeSpan.FromSeconds(15));

            Assert.That(result, Is.EqualTo(50));
        }

        [Test]
        public void ThenItReturnsToZeroAtTheEnd()
        {
            var profile = RampProfile(100, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

            var result = profile.TargetUsersAt(TimeSpan.FromSeconds(20));

            Assert.That(result, Is.EqualTo(0));
        }
    }
}