using Pacer.Scenarios;

namespace Pacer.Tests.Scenarios.WhenBuildingAScenario;

public partial class WhenBuildingAScenario
{
    public class AndTheConfigurationIsValid : ScenarioTestBase
    {
        [Test]
        public void ThenItPreservesTheStepOrder()
        {
            var definition = Scenario.Create("checkout")
                .AddStep("login", AnyStep())
                .AddStep("browse", AnyStep())
                .AddStep("purchase", AnyStep())
                .WithLoad(AnyLoad())
                .Build();

            var stepNames = definition.Steps.Select(s => s.Name).ToArray();

            Assert.That(stepNames, Is.EqualTo(new[] { "login", "browse", "purchase" }));
        }

        [Test]
        public void ThenItRecordsTheGroup()
        {
            var definition = Scenario.Create("checkout")
                .InGroup("ecommerce")
                .AddStep("login", AnyStep())
                .WithLoad(AnyLoad())
                .Build();

            Assert.That(definition.Group, Is.EqualTo("ecommerce"));
        }

        [Test]
        public void ThenItDefaultsWarmupToZero()
        {
            var definition = Scenario.Create("checkout")
                .AddStep("login", AnyStep())
                .WithLoad(AnyLoad())
                .Build();

            Assert.That(definition.Warmup, Is.EqualTo(TimeSpan.Zero));
        }
    }
}