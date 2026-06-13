using Pacer.Hosting;
using Pacer.Load;
using Pacer.Scenarios;
using Pacer.Steps;

namespace Pacer.Tests.Hosting.WhenPlanningARun;

public partial class WhenPlanningARun
{
    public class AndApplyingOverrides
    {
        private static ScenarioDefinition Scenario()
        {
            return Pacer.Scenarios.Scenario.Create("checkout")
                .AddStep("step", (_, _) => ValueTask.FromResult(StepResult.Ok()))
                .WithLoad(LoadProfiles.Load(10, TimeSpan.FromSeconds(30)))
                .Build();
        }

        [Test]
        public void ThenItReturnsTheSameScenarioWhenNothingIsOverridden()
        {
            var scenario = Scenario();

            var result = RunPlanner.Apply(scenario, new RunOptions());

            Assert.That(result, Is.SameAs(scenario));
        }

        [Test]
        public void ThenOverridingUsersChangesThePeak()
        {
            var result = RunPlanner.Apply(Scenario(), new RunOptions { Users = 200 });

            Assert.That(result.Load.PeakUsers, Is.EqualTo(200));
        }

        [Test]
        public void ThenOverridingTheProfileChangesTheKind()
        {
            var result = RunPlanner.Apply(Scenario(), new RunOptions { Profile = "ramp", Users = 100, Duration = TimeSpan.FromSeconds(60) });

            Assert.That(result.Load.Kind, Is.EqualTo("Ramp"));
        }

        [Test]
        public void ThenOverridingTheDurationChangesTheTotal()
        {
            var result = RunPlanner.Apply(Scenario(), new RunOptions { Duration = TimeSpan.FromSeconds(90) });

            Assert.That(result.Load.TotalDuration, Is.EqualTo(TimeSpan.FromSeconds(90)));
        }

        [Test]
        public void ThenAnUnknownProfileThrows()
        {
            Assert.That(
                () => RunPlanner.Apply(Scenario(), new RunOptions { Profile = "rocket" }),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void ThenOverridingTheWarmupIsApplied()
        {
            var result = RunPlanner.Apply(Scenario(), new RunOptions { Warmup = TimeSpan.FromSeconds(5) });

            Assert.That(result.Warmup, Is.EqualTo(TimeSpan.FromSeconds(5)));
        }
    }
}