namespace Pacer.Tests.Reporting.WhenWritingReportFiles;

public class ReportFileWriterTestBase
{
    protected string OutputDirectory { get; private set; } = null!;

    [SetUp]
    public void SetUp()
    {
        this.OutputDirectory = Path.Combine(Path.GetTempPath(), "pacer-tests-" + Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(this.OutputDirectory))
        {
            Directory.Delete(this.OutputDirectory, recursive: true);
        }
    }
}
