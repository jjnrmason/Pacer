using Pacer.Metrics;

namespace Pacer.Reporting;

/// <summary>
/// Writes one or more <see cref="RunReport"/>s to a destination (console, CSV, HTML, …). Implement
/// this and register it with <c>AddPacer().AddReportWriter&lt;T&gt;()</c> to add a custom output
/// format; every registered writer runs after each test run.
/// </summary>
/// <example>
/// <code><![CDATA[
/// public sealed class JsonReportWriter : IReportWriter
/// {
///     public string Format => "json";
///
///     public async Task WriteAsync(IReadOnlyList<RunReport> reports, string outputDirectory, CancellationToken ct = default)
///     {
///         Directory.CreateDirectory(outputDirectory);
///         foreach (var report in reports)
///         {
///             var path = Path.Combine(outputDirectory, $"{report.ScenarioName}.json");
///             await File.WriteAllTextAsync(path, JsonSerializer.Serialize(report), ct);
///         }
///     }
/// }
/// ]]></code>
/// </example>
public interface IReportWriter
{
    /// <summary>The format identifier this writer handles (e.g. "console", "csv", "html").</summary>
    string Format { get; }

    /// <summary>Writes the reports. File-based writers emit into <paramref name="outputDirectory"/>.</summary>
    Task WriteAsync(IReadOnlyList<RunReport> reports, string outputDirectory, CancellationToken cancellationToken = default);
}
