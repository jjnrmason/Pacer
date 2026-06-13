using Pacer.Load;

namespace Pacer.Tests.Load.WhenSamplingALoadProfile;

public class LoadProfileTestBase
{
    protected static LoadProfile ConstantProfile(int users, TimeSpan duration)
    {
        return new LoadProfile("Test", [new LoadStage(users, users, duration)]);
    }

    protected static LoadProfile RampProfile(int peak, TimeSpan rampUp, TimeSpan rampDown)
    {
        return new LoadProfile("Test", [
            new LoadStage(0, peak, rampUp),
            new LoadStage(peak, 0, rampDown),
        ]);
    }
}