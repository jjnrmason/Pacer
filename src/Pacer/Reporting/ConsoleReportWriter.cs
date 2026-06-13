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
    private static readonly string[] Headers = ["step", "ok", "fail", "rps", "in", "out", "mean ms", "p95 ms", "p99 ms", "max ms"];

    // Column indices whose values are coloured in data rows.
    private const int OkColumn = 1;
    private const int FailColumn = 2;

    private const string AnsiGreen = "\u001b[32m";
    private const string AnsiRed = "\u001b[31m";
    private const string AnsiReset = "\u001b[0m";

    /// <summary>
    /// Whether ANSI colour is emitted. Disabled when output is redirected (e.g. piped to a file) or
    /// when the conventional <c>NO_COLOR</c> environment variable is set.
    /// </summary>
    private static bool ColorEnabled =>
        !Console.IsOutputRedirected && Environment.GetEnvironmentVariable("NO_COLOR") is null;

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
        var colorize = ColorEnabled;
        sb.AppendLine(FormatRow(Headers, widths, colorize: false));
        sb.AppendLine(light);
        foreach (var row in rows)
            sb.AppendLine(FormatRow(row, widths, colorize));
        sb.AppendLine(light);
        sb.AppendLine(FormatRow(journey, widths, colorize));
        sb.Append(heavy);
        return sb.ToString();
    }

    private static string[] Cells(StepStats step) =>
    [
        step.Name,
        ReportFormatting.IntGrouped(step.Ok),
        ReportFormatting.IntGrouped(step.Fail),
        ReportFormatting.Rps(step.RequestsPerSecond),
        ReportFormatting.Bytes(step.BytesReceived),
        ReportFormatting.Bytes(step.BytesSent),
        ReportFormatting.Ms(step.MeanMs),
        ReportFormatting.Ms(step.P95Ms),
        ReportFormatting.Ms(step.P99Ms),
        ReportFormatting.Ms(step.MaxMs),
    ];

    private static string FormatRow(IReadOnlyList<string> cells, int[] widths, bool colorize)
    {
        var sb = new StringBuilder();
        sb.Append(' ');
        for (var i = 0; i < cells.Count; i++)
        {
            if (i > 0)
                sb.Append(' ', ColumnGap);

            // First column (the step name) is left-aligned; numeric columns are right-aligned.
            var cell = i == 0 ? cells[i].PadRight(widths[i]) : cells[i].PadLeft(widths[i]);

            // Colour is applied after padding so the escape codes don't disturb column alignment.
            // A zero count is left uncoloured so a clean run isn't a wall of red zeros.
            if (colorize && cells[i] != "0")
            {
                if (i == OkColumn)
                    cell = $"{AnsiGreen}{cell}{AnsiReset}";
                else if (i == FailColumn)
                    cell = $"{AnsiRed}{cell}{AnsiReset}";
            }

            sb.Append(cell);
        }

        return sb.ToString();
    }
}
