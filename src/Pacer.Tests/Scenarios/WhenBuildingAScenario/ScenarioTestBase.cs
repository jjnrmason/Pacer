using Pacer.Load;
using Pacer.Steps;

namespace Pacer.Tests.Scenarios.WhenBuildingAScenario;

public class ScenarioTestBase
{
    protected static LoadProfile AnyLoad()
    {
        return LoadProfiles.Load(10, TimeSpan.FromSeconds(5));
    }

    protected static StepDelegate AnyStep()
    {
        return (_, _) => ValueTask.FromResult(StepResult.Ok());
    }
}