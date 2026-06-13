using Pacer.Reporting;

namespace Pacer.Tests.Reporting.WhenRenderingHtml;

public partial class WhenRenderingHtml
{
    public class AndRenderingTheDocument
    {
        [Test]
        public void ThenItProducesAnHtmlDocument()
        {
            var html = HtmlReportWriter.Render([ReportFixtures.Report()]);

            Assert.That(html, Does.StartWith("<!DOCTYPE html>"));
        }

        [Test]
        public void ThenItIncludesTheScenarioName()
        {
            var html = HtmlReportWriter.Render([ReportFixtures.Report()]);

            Assert.That(html, Does.Contain("checkout"));
        }

        [Test]
        public void ThenItRendersAResultsTable()
        {
            var html = HtmlReportWriter.Render([ReportFixtures.Report()]);

            Assert.That(html, Does.Contain("<table>"));
        }

        [Test]
        public void ThenItRendersAThroughputSparklineWhenIntervalsExist()
        {
            var html = HtmlReportWriter.Render([ReportFixtures.Report()]);

            Assert.That(html, Does.Contain("<svg"));
        }

        [Test]
        public void ThenItEncodesScenarioNamesToPreventInjection()
        {
            var report = ReportFixtures.Report(scenarioName: "<script>alert(1)</script>");

            var html = HtmlReportWriter.Render([report]);

            Assert.That(html, Does.Not.Contain("<script>alert(1)</script>"));
        }

        [Test]
        public void ThenItIncludesTheEnvironmentMetadata()
        {
            var html = HtmlReportWriter.Render([ReportFixtures.Report()]);

            Assert.That(html, Does.Contain("test-machine"));
        }
    }
}