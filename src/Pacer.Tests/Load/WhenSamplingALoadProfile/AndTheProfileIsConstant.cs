namespace Pacer.Tests.Load.WhenSamplingALoadProfile;

public partial class WhenSamplingALoadProfile
{
    public class AndTheProfileIsConstant : LoadProfileTestBase
    {
        [Test]
        public void ThenItReturnsTheConstantCountBeforeTheStart()
        {
            var profile = ConstantProfile(50, TimeSpan.FromSeconds(10));

            var result = profile.TargetUsersAt(TimeSpan.FromSeconds(-1));

            Assert.That(result, Is.EqualTo(50));
        }

        [Test]
        public void ThenItReturnsTheConstantCountPartWayThrough()
        {
            var profile = ConstantProfile(50, TimeSpan.FromSeconds(10));

            var result = profile.TargetUsersAt(TimeSpan.FromSeconds(5));

            Assert.That(result, Is.EqualTo(50));
        }

        [Test]
        public void ThenItReturnsTheConstantCountPastTheEnd()
        {
            var profile = ConstantProfile(50, TimeSpan.FromSeconds(10));

            var result = profile.TargetUsersAt(TimeSpan.FromSeconds(99));

            Assert.That(result, Is.EqualTo(50));
        }
    }
}