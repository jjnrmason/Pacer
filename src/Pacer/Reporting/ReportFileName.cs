using Pacer.Metrics;

namespace Pacer.Reporting;

/// <summary>
/// Builds the base file name for a report: the scenario title followed by the run's timestamp, e.g.
/// <c>checkout-20260613-100000</c>. Writers append their own extension.
/// </summary>
internal static class ReportFileName
{
    public static string ForScenario(RunReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return $"{Sanitize(report.ScenarioName)}-{report.StartedAt:yyyyMMdd-HHmmss}";
    }

    private static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(c => invalid.Contains(c) || char.IsWhiteSpace(c) ? '_' : c).ToArray();
        return new string(chars);
    }
}
