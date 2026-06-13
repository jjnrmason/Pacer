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
        // Palette and styling mirror the Pacer docs site (docs/index.html) for a consistent look.
        sb.AppendLine(":root{--bg:#0a0d14;--panel:#111722;--panel2:#0e141d;--line:#1e2733;--text:#e6edf3;--muted:#93a1b5;--dim:#62718a;--accent:#7c8bff;--accent2:#22d3ee;--ok:#34d399;--fail:#fb7185;--radius:16px;--grad:linear-gradient(120deg,#7c8bff,#22d3ee)}");
        sb.AppendLine("*{box-sizing:border-box}");
        sb.AppendLine("body{margin:0;padding:2.5rem 1.5rem;background:var(--bg);color:var(--text);line-height:1.6;-webkit-font-smoothing:antialiased;font-family:ui-sans-serif,system-ui,-apple-system,\"Segoe UI\",Roboto,Inter,sans-serif;background-image:radial-gradient(60rem 40rem at 75% -10%,rgba(124,139,255,.16),transparent 60%),radial-gradient(50rem 40rem at 10% 0%,rgba(34,211,238,.12),transparent 55%);background-repeat:no-repeat}");
        sb.AppendLine(".wrap{max-width:1080px;margin:0 auto}");
        sb.AppendLine("h1{font-size:clamp(1.8rem,4vw,2.4rem);letter-spacing:-.03em;font-weight:820;margin:0 0 1.75rem}");
        sb.AppendLine("h1 .grad{background:var(--grad);-webkit-background-clip:text;background-clip:text;color:transparent}");
        sb.AppendLine("h2{font-size:1.25rem;letter-spacing:-.01em;margin:2.4rem 0 .4rem;font-weight:750}");
        sb.AppendLine("h2 .group{color:var(--dim);font-weight:600;font-size:.9rem}");
        sb.AppendLine(".meta{color:var(--muted);font-size:.85rem;margin-bottom:1rem}");
        sb.AppendLine("table{border-collapse:collapse;width:100%;background:var(--panel);border:1px solid var(--line);border-radius:var(--radius);overflow:hidden;font-variant-numeric:tabular-nums}");
        sb.AppendLine("th,td{padding:.5rem .85rem;text-align:right;border-bottom:1px solid var(--line)}");
        sb.AppendLine("th:first-child,td:first-child{text-align:left}");
        sb.AppendLine("thead th{background:var(--panel2);color:var(--muted);font-weight:600;font-size:.74rem;text-transform:uppercase;letter-spacing:.06em}");
        sb.AppendLine("tbody tr:last-child td{border-bottom:none}");
        sb.AppendLine("tr.total td{font-weight:700;border-top:2px solid #2c3a4e}");
        sb.AppendLine("td.ok{color:var(--ok)}td.fail{color:var(--fail)}td.zero{color:var(--dim)}");
        sb.AppendLine(".spark{margin:.5rem 0}");
        sb.AppendLine("</style></head><body><div class=\"wrap\">");
        sb.AppendLine("<h1>Pacer <span class=\"grad\">performance report</span></h1>");

        foreach (var report in reports)
            AppendScenario(sb, report);

        sb.AppendLine("</div></body></html>");
        return sb.ToString();
    }

    private static void AppendScenario(StringBuilder sb, RunReport report)
    {
        var group = report.Group is null ? "" : $" <span class=\"group\">[group: {Encode(report.Group)}]</span>";
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
        foreach (var header in new[] { "step", "ok", "fail", "rps", "in", "out", "min ms", "mean ms", "p50 ms", "p95 ms", "p99 ms", "p99.9 ms", "max ms" })
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
        // ok in green, fail in red; a zero count is dimmed rather than coloured.
        sb.Append(step.Ok > 0 ? "<td class=\"ok\">" : "<td class=\"zero\">").Append(ReportFormatting.Int(step.Ok)).Append("</td>");
        sb.Append(step.Fail > 0 ? "<td class=\"fail\">" : "<td class=\"zero\">").Append(ReportFormatting.Int(step.Fail)).Append("</td>");
        sb.Append("<td>").Append(ReportFormatting.Rps(step.RequestsPerSecond)).Append("</td>");
        sb.Append("<td>").Append(ReportFormatting.Bytes(step.BytesReceived)).Append("</td>");
        sb.Append("<td>").Append(ReportFormatting.Bytes(step.BytesSent)).Append("</td>");
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
        sb.Append("<defs><linearGradient id=\"spark\" x1=\"0\" y1=\"0\" x2=\"1\" y2=\"0\">")
          .Append("<stop offset=\"0\" stop-color=\"#7c8bff\"/><stop offset=\"1\" stop-color=\"#22d3ee\"/></linearGradient></defs>");
        sb.Append("<polyline fill=\"none\" stroke=\"url(#spark)\" stroke-width=\"2\" stroke-linejoin=\"round\" stroke-linecap=\"round\" points=\"")
          .Append(points.ToString().TrimEnd()).Append("\"/>");
        sb.Append("</svg>");
        sb.AppendLine();
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
