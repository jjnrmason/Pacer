namespace Pacer.Tests.Scenarios.WhenUsingTheScenarioRegistry;

public partial class WhenUsingTheScenarioRegistry
{
    public class AndLookingUpScenarios : ScenarioRegistryTestBase
    {
        [Test]
        public void ThenItFindsAScenarioByNameCaseInsensitively()
        {
            this.ScenarioRegistry.Add(Definition("Checkout"));

            var result = this.ScenarioRegistry.Get("checkout");

            Assert.That(result.Name, Is.EqualTo("Checkout"));
        }

        [Test]
        public void ThenItThrowsWhenAScenarioIsNotFound()
        {
            Assert.That(() => this.ScenarioRegistry.Get("missing"), Throws.TypeOf<KeyNotFoundException>());
        }

        [Test]
        public void ThenItThrowsWhenADuplicateNameIsAdded()
        {
            this.ScenarioRegistry.Add(Definition("checkout"));

            Assert.That(() => this.ScenarioRegistry.Add(Definition("checkout")), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void ThenItReturnsOnlyScenariosInTheRequestedGroup()
        {
            this.ScenarioRegistry.Add(Definition("a", group: "g1"));
            this.ScenarioRegistry.Add(Definition("b", group: "g2"));
            this.ScenarioRegistry.Add(Definition("c", group: "g1"));

            var result = this.ScenarioRegistry.InGroup("g1").Select(s => s.Name).OrderBy(n => n);

            Assert.That(result, Is.EqualTo(new[] { "a", "c" }));
        }

        [Test]
        public void ThenItListsTheDistinctGroups()
        {
            this.ScenarioRegistry.Add(Definition("a", group: "g1"));
            this.ScenarioRegistry.Add(Definition("b", group: "g1"));
            this.ScenarioRegistry.Add(Definition("c"));

            Assert.That(this.ScenarioRegistry.Groups, Is.EquivalentTo(new[] { "g1" }));
        }
    }
}