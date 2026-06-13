using Pacer.Load;
using Pacer.Scenarios;
using Pacer.Steps;

namespace Pacer.Tests.Engine.WhenRunningAScenario;

public partial class WhenRunningAScenario
{
    public class AndUsingInlineSetup : ScenarioRunnerTestBase
    {
        [Test]
        public async Task ThenTheSetupDataIsVisibleToSteps()
        {
            object? seenData = null;
            var scenario = Scenario.Create("with-setup")
                .WithSetup(_ => ValueTask.FromResult("seeded-token"))
                .AddStep("step", ctx =>
                {
                    seenData = ctx.ScenarioData;
                    return ValueTask.FromResult(StepResult.Ok());
                })
                .WithLoad(LoadProfiles.Load(2, TimeSpan.FromSeconds(10)))
                .Build();

            await RunToCompletionAsync(scenario);

            Assert.That(seenData, Is.EqualTo("seeded-token"));
        }

        [Test]
        public async Task ThenTeardownRunsAfterTheLoadPhase()
        {
            var torndown = false;
            var scenario = Scenario.Create("with-teardown")
                .AddStep("step", _ => ValueTask.FromResult(StepResult.Ok()))
                .WithTeardown((_, _) =>
                {
                    torndown = true;
                    return ValueTask.CompletedTask;
                })
                .WithLoad(LoadProfiles.Load(2, TimeSpan.FromSeconds(10)))
                .Build();

            await RunToCompletionAsync(scenario);

            Assert.That(torndown, Is.True);
        }
    }
}
