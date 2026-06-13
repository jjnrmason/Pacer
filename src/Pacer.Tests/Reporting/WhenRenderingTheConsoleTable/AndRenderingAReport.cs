using Pacer.Reporting;

namespace Pacer.Tests.Reporting.WhenRenderingTheConsoleTable;

public partial class WhenRenderingTheConsoleTable
{
    public class AndRenderingAReport
    {
        [Test]
        public void ThenItShowsTheScenarioName()
        {
            var table = ConsoleReportWriter.Render(ReportFixtures.Report());

            Assert.That(table, Does.Contain("Scenario: checkout"));
        }

        [Test]
        public void ThenItShowsTheLoadProfileKind()
        {
            var table = ConsoleReportWriter.Render(ReportFixtures.Report());

            Assert.That(table, Does.Contain("Profile:").And.Contain("Ramp"));
        }

        [Test]
        public void ThenItListsEachStep()
        {
            var table = ConsoleReportWriter.Render(ReportFixtures.Report());

            Assert.That(table, Does.Contain("login"));
        }
    }
}