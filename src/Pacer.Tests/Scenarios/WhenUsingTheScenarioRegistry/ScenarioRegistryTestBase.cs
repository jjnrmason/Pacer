using Pacer.Load;
using Pacer.Scenarios;
using Pacer.Steps;

namespace Pacer.Tests.Scenarios.WhenUsingTheScenarioRegistry;

public class ScenarioRegistryTestBase
{
    protected ScenarioRegistry ScenarioRegistry { get; private set; } = null!;

    [SetUp]
    public void SetUp()
    {
        this.ScenarioRegistry = new ScenarioRegistry();
    }

    protected static ScenarioDefinition Definition(string name, string? group = null)
    {
        var builder = Scenario.Create(name)
            .AddStep("step", (_, _) => ValueTask.FromResult(StepResult.Ok()))
            .WithLoad(LoadProfiles.Load(1, TimeSpan.FromSeconds(1)));
        if (group is not null)
        {
            builder.InGroup(group);
        }

        return builder.Build();
    }
}