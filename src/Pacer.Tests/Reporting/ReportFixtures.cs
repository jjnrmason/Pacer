using Pacer.Metrics;

namespace Pacer.Tests.Reporting;

internal static class ReportFixtures
{
    public static StepStats Step(string name, long ok = 100, long fail = 5)
    {
        return new StepStats
        {
            Name = name,
            Ok = ok,
            Fail = fail,
            BytesSent = 512,
            BytesReceived = 1536,
            RequestsPerSecond = 10.5,
            MinMs = 1.0,
            MeanMs = 5.25,
            MaxMs = 50.0,
            P50Ms = 4.0,
            P75Ms = 6.0,
            P95Ms = 20.0,
            P99Ms = 40.0,
            P999Ms = 49.0,
        };
    }

    public static RunReport Report(string scenarioName = "checkout", string? group = "ecommerce", params StepStats[] steps)
    {
        var stepList = steps.Length > 0 ? steps : [Step("login"), Step("purchase")];
        return new RunReport
        {
            ScenarioName = scenarioName,
            Group = group,
            LoadProfileKind = "Ramp",
            PeakUsers = 200,
            StartedAt = new DateTimeOffset(2026, 6, 13, 10, 0, 0, TimeSpan.Zero),
            Duration = TimeSpan.FromSeconds(60),
            Steps = stepList,
            Journey = Step(scenarioName, ok: 95, fail: 5),
            Intervals = [
                new IntervalSnapshot(TimeSpan.FromSeconds(1), 50, 40, 1, 41),
                new IntervalSnapshot(TimeSpan.FromSeconds(2), 100, 90, 2, 49),
                new IntervalSnapshot(TimeSpan.FromSeconds(3), 200, 180, 4, 91),
            ],
            Environment = new EnvironmentInfo("TestOS", ".NET 10.0", 8, "test-machine"),
        };
    }
}