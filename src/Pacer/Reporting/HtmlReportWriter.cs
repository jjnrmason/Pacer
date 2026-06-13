using System.Net;
using System.Text;
using Pacer.Metrics;

namespace Pacer.Reporting;

/// <summary>
/// Writes a single self-contained HTML report (inline CSS, an inline SVG throughput sparkline, no
/// external scripts or styles) summarising every scenario in the run.
/// </summary>
public sealed class HtmlReportWriter : IReportWriter
{
    /// <inheritdoc />
    public string Format => "html";

    /// <inheritdoc />
    public async Task WriteAsync(IReadOnlyList<RunReport> reports, string outputDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reports);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        Directory.CreateDirectory(outputDirectory);

        foreach (var report in reports)
        {
            var fileName = $"{ReportFileName.ForScenario(report)}.html";
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, fileName), Render([report]), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Renders the complete HTML document for the given reports.</summary>
    public static string Render(IReadOnlyList<RunReport> reports)
    {
        ArgumentNullException.ThrowIfNull(reports);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.AppendLine("<title>Pacer report</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:system-ui,Segoe UI,Roboto,sans-serif;margin:2rem;color:#1a1a1a;background:#fafafa}");
        sb.AppendLine("h1{font-size:1.6rem}h2{font-size:1.2rem;margin-top:2rem}");
        sb.AppendLine(".meta{color:#555;font-size:.85rem;margin-bottom:1rem}");
        sb.AppendLine("table{border-collapse:collapse;width:100%;background:#fff;box-shadow:0 1px 3px rgba(0,0,0,.1)}");
        sb.AppendLine("th,td{padding:.5rem .75rem;text-align:right;border-bottom:1px solid #eee;font-variant-numeric:tabular-nums}");
        sb.AppendLine("th:first-child,td:first-child{text-align:left}");
        sb.AppendLine("th{background:#f0f0f0;font-weight:600}");
        sb.AppendLine("tr.total td{font-weight:700;border-top:2px solid #ccc}");
        sb.AppendLine(".spark{margin:.5rem 0}");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine("<h1>Pacer performance report</h1>");

        foreach (var report in reports)
            AppendScenario(sb, report);

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static void AppendScenario(StringBuilder sb, RunReport report)
    {
        var group = report.Group is null ? "" : $" <span class=\"meta\">[group: {Encode(report.Group)}]</span>";
        sb.Append("<h2>").Append(Encode(report.ScenarioName)).Append(group).AppendLine("</h2>");

        sb.Append("<div class=\"meta\">")
          .Append("Profile: ").Append(Encode(report.LoadProfileKind))
          .Append(" &middot; Peak users: ").Append(report.PeakUsers)
          .Append(" &middot; Duration: ").Append(ReportFormatting.Seconds(report.Duration)).Append("s")
          .Append(" &middot; Started: ").Append(Encode(report.StartedAt.ToString("u")))
          .Append("<br>").Append(Encode(report.Environment.OperatingSystem))
          .Append(" &middot; ").Append(Encode(report.Environment.RuntimeVersion))
          .Append(" &middot; ").Append(report.Environment.ProcessorCount).Append(" CPUs")
          .Append(" &middot; ").Append(Encode(report.Environment.MachineName))
          .AppendLine("</div>");

        AppendSparkline(sb, report);

        sb.AppendLine("<table><thead><tr>");
        foreach (var header in new[] { "step", "ok", "fail", "rps", "min ms", "mean ms", "p50 ms", "p95 ms", "p99 ms", "p99.9 ms", "max ms" })
            sb.Append("<th>").Append(header).Append("</th>");
        sb.AppendLine("</tr></thead><tbody>");

        foreach (var step in report.Steps)
            AppendRow(sb, step, isTotal: false);
        AppendRow(sb, report.Journey, isTotal: true);

        sb.AppendLine("</tbody></table>");
    }

    private static void AppendRow(StringBuilder sb, StepStats step, bool isTotal)
    {
        sb.Append(isTotal ? "<tr class=\"total\">" : "<tr>");
        sb.Append("<td>").Append(Encode(step.Name)).Append("</td>");
        sb.Append("<td>").Append(ReportFormatting.Int(step.Ok)).Append("</td>");
        sb.Append("<td>").Append(ReportFormatting.Int(step.Fail)).Append("</td>");
        sb.Append("<td>").Append(ReportFormatting.Rps(step.RequestsPerSecond)).Append("</td>");
        sb.Append("<td>").Append(ReportFormatting.Ms(step.MinMs)).Append("</td>");
        sb.Append("<td>").Append(ReportFormatting.Ms(step.MeanMs)).Append("</td>");
        sb.Append("<td>").Append(ReportFormatting.Ms(step.P50Ms)).Append("</td>");
        sb.Append("<td>").Append(ReportFormatting.Ms(step.P95Ms)).Append("</td>");
        sb.Append("<td>").Append(ReportFormatting.Ms(step.P99Ms)).Append("</td>");
        sb.Append("<td>").Append(ReportFormatting.Ms(step.P999Ms)).Append("</td>");
        sb.Append("<td>").Append(ReportFormatting.Ms(step.MaxMs)).Append("</td>");
        sb.AppendLine("</tr>");
    }

    private static void AppendSparkline(StringBuilder sb, RunReport report)
    {
        if (report.Intervals.Count < 2)
            return;

        const int width = 600;
        const int height = 80;
        var maxRps = report.Intervals.Max(i => i.RequestsPerSecond);
        if (maxRps <= 0)
            return;

        var lastElapsed = report.Intervals[^1].Elapsed.TotalSeconds;
        if (lastElapsed <= 0)
            return;

        var points = new StringBuilder();
        foreach (var interval in report.Intervals)
        {
            var x = interval.Elapsed.TotalSeconds / lastElapsed * width;
            var y = height - interval.RequestsPerSecond / maxRps * height;
            points.Append(ReportFormatting.Ms(x)).Append(',').Append(ReportFormatting.Ms(y)).Append(' ');
        }

        sb.Append("<svg class=\"spark\" viewBox=\"0 0 ").Append(width).Append(' ').Append(height)
          .Append("\" width=\"").Append(width).Append("\" height=\"").Append(height)
          .Append("\" role=\"img\" aria-label=\"throughput over time\">");
        sb.Append("<polyline fill=\"none\" stroke=\"#2563eb\" stroke-width=\"2\" points=\"")
          .Append(points.ToString().TrimEnd()).Append("\"/>");
        sb.Append("</svg>");
        sb.AppendLine();
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
