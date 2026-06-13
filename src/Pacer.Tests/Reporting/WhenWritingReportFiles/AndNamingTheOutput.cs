using Pacer.Reporting;

namespace Pacer.Tests.Reporting.WhenWritingReportFiles;

public partial class WhenWritingReportFiles
{
    public class AndNamingTheOutput : ReportFileWriterTestBase
    {
        [Test]
        public async Task ThenTheCsvIsNamedAfterTheScenarioAndTimestamp()
        {
            await new CsvReportWriter().WriteAsync([ReportFixtures.Report()], this.OutputDirectory);

            Assert.That(File.Exists(Path.Combine(this.OutputDirectory, "checkout-20260613-100000.csv")), Is.True);
        }

        [Test]
        public async Task ThenTheHtmlIsNamedAfterTheScenarioAndTimestamp()
        {
            await new HtmlReportWriter().WriteAsync([ReportFixtures.Report()], this.OutputDirectory);

            Assert.That(File.Exists(Path.Combine(this.OutputDirectory, "checkout-20260613-100000.html")), Is.True);
        }

        [Test]
        public async Task ThenItWritesOneCsvPerScenario()
        {
            var reports = new[] { ReportFixtures.Report("checkout"), ReportFixtures.Report("search") };

            await new CsvReportWriter().WriteAsync(reports, this.OutputDirectory);

            var summaries = Directory.GetFiles(this.OutputDirectory, "*.csv")
                .Select(Path.GetFileName)
                .Where(f => !f!.Contains("intervals"))
                .ToArray();
            Assert.That(summaries, Has.Length.EqualTo(2));
        }

        [Test]
        public async Task ThenItWritesADefaultReportsDirectoryRelativeName()
        {
            await new CsvReportWriter().WriteAsync([ReportFixtures.Report()], this.OutputDirectory);

            Assert.That(File.Exists(Path.Combine(this.OutputDirectory, "checkout-20260613-100000-intervals.csv")), Is.True);
        }
    }
}
