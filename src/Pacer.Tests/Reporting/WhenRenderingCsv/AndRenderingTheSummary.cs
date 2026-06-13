using Pacer.Reporting;

namespace Pacer.Tests.Reporting.WhenRenderingCsv;

public partial class WhenRenderingCsv
{
    public class AndRenderingTheSummary
    {
        [Test]
        public void ThenItStartsWithTheHeaderRow()
        {
            var csv = CsvReportWriter.RenderSummary([ReportFixtures.Report()]);

            var firstLine = csv.Split('\n')[0].Trim();
            Assert.That(firstLine, Is.EqualTo("scenario,row,ok,fail,total,rps,min_ms,mean_ms,p50_ms,p75_ms,p95_ms,p99_ms,p999_ms,max_ms,bytes_sent,bytes_received"));
        }

        [Test]
        public void ThenItWritesARowForEachStep()
        {
            var csv = CsvReportWriter.RenderSummary([ReportFixtures.Report()]);

            Assert.That(csv, Does.Contain("checkout,login,100,5,105,"));
        }

        [Test]
        public void ThenItWritesAJourneyTotalRow()
        {
            var csv = CsvReportWriter.RenderSummary([ReportFixtures.Report()]);

            Assert.That(csv, Does.Contain("checkout,checkout,95,5,100,"));
        }

        [Test]
        public void ThenItQuotesFieldsContainingAComma()
        {
            var report = ReportFixtures.Report(steps: new[] { ReportFixtures.Step("a,b") });

            var csv = CsvReportWriter.RenderSummary([report]);

            Assert.That(csv, Does.Contain("\"a,b\""));
        }

        [Test]
        public void ThenItFormatsLatencyWithInvariantDecimalPoints()
        {
            var csv = CsvReportWriter.RenderSummary([ReportFixtures.Report()]);

            Assert.That(csv, Does.Contain("5.25"));
        }
    }
}