using System.Text;
using Pacer.Metrics;

namespace Pacer.Reporting;

/// <summary>
/// Renders each report as an aligned, boxed text table and writes it directly to the console so the
/// layout is preserved (it deliberately bypasses the logger, which would collapse the rows).
/// </summary>
public sealed class ConsoleReportWriter : IReportWriter
{
    private const int ColumnGap = 2;
    private static readonly string[] Headers = ["step", "ok", "fail", "rps", "mean ms", "p95 ms", "p99 ms", "max ms"];

    /// <inheritdoc />
    public string Format => "console";

    /// <inheritdoc />
    public Task WriteAsync(IReadOnlyList<RunReport> reports, string outputDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reports);
        foreach (var report in reports)
        {
            Console.Out.WriteLine();
            Console.Out.WriteLine(Render(report));
        }

        return Task.CompletedTask;
    }

    /// <summary>Renders a single report as an aligned table.</summary>
    public static string Render(RunReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var rows = report.Steps.Select(Cells).ToList();
        var journey = Cells(report.Journey);

        var widths = new int[Headers.Length];
        for (var i = 0; i < Headers.Length; i++)
            widths[i] = Headers[i].Length;
        foreach (var row in rows.Append(journey))
        {
            for (var i = 0; i < Headers.Length; i++)
                widths[i] = Math.Max(widths[i], row[i].Length);
        }

        var group = report.Group is null ? "" : $"  [group: {report.Group}]";
        var titleLine = $" Scenario: {report.ScenarioName}{group}";
        var profileLine = $" Profile:  {report.LoadProfileKind}  ·  {report.PeakUsers} peak users  ·  {ReportFormatting.HumanDuration(report.Duration)}";

        var tableWidth = widths.Sum() + (ColumnGap * (Headers.Length - 1)) + 1;
        var ruleWidth = Math.Max(tableWidth, Math.Max(titleLine.Length, profileLine.Length));
        var heavy = new string('═', ruleWidth);
        var light = new string('─', ruleWidth);

        var sb = new StringBuilder();
        sb.AppendLine(heavy);
        sb.AppendLine(titleLine);
        sb.AppendLine(profileLine);
        sb.AppendLine(heavy);
        sb.AppendLine(FormatRow(Headers, widths));
        sb.AppendLine(light);
        foreach (var row in rows)
            sb.AppendLine(FormatRow(row, widths));
        sb.AppendLine(light);
        sb.AppendLine(FormatRow(journey, widths));
        sb.Append(heavy);
        return sb.ToString();
    }

    private static string[] Cells(StepStats step) =>
    [
        step.Name,
        ReportFormatting.IntGrouped(step.Ok),
        ReportFormatting.IntGrouped(step.Fail),
        ReportFormatting.Rps(step.RequestsPerSecond),
        ReportFormatting.Ms(step.MeanMs),
        ReportFormatting.Ms(step.P95Ms),
        ReportFormatting.Ms(step.P99Ms),
        ReportFormatting.Ms(step.MaxMs),
    ];

    private static string FormatRow(IReadOnlyList<string> cells, int[] widths)
    {
        var sb = new StringBuilder();
        sb.Append(' ');
        for (var i = 0; i < cells.Count; i++)
        {
            if (i > 0)
                sb.Append(' ', ColumnGap);

            // First column (the step name) is left-aligned; numeric columns are right-aligned.
            sb.Append(i == 0 ? cells[i].PadRight(widths[i]) : cells[i].PadLeft(widths[i]));
        }

        return sb.ToString();
    }
}
