using Pacer.Metrics;

namespace Pacer.Tests.Metrics.WhenCollectingStepStats;

public class StepStatsCollectorTestBase
{
    protected StepStatsCollector StepStatsCollector { get; private set; } = null!;

    [SetUp]
    public void SetUp()
    {
        this.StepStatsCollector = new StepStatsCollector("login");
    }
}