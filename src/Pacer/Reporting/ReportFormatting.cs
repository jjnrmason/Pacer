using System.Globalization;

namespace Pacer.Reporting;

/// <summary>Shared, culture-invariant formatting helpers used by the report writers.</summary>
internal static class ReportFormatting
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static string Ms(double value) => value.ToString("F2", Invariant);

    public static string Rps(double value) => value.ToString("F1", Invariant);

    public static string Int(long value) => value.ToString(Invariant);

    public static string IntGrouped(long value) => value.ToString("N0", Invariant);

    /// <summary>A compact human-friendly byte size, e.g. "512 B", "4.0 KB" or "1.5 MB" (1024-based).</summary>
    public static string Bytes(long value)
    {
        if (value < 1024)
            return $"{value} B";

        double scaled = value;
        string[] units = ["KB", "MB", "GB", "TB", "PB"];
        var unit = -1;
        do
        {
            scaled /= 1024;
            unit++;
        }
        while (scaled >= 1024 && unit < units.Length - 1);

        return $"{scaled.ToString("0.0", Invariant)} {units[unit]}";
    }

    /// <summary>A compact human-friendly duration, e.g. "45.0s" or "2m 30s".</summary>
    public static string HumanDuration(TimeSpan value)
        => value.TotalSeconds < 60
            ? $"{value.TotalSeconds.ToString("0.#", Invariant)}s"
            : $"{(int)value.TotalMinutes}m {value.Seconds:00}s";

    public static string Seconds(TimeSpan value) => value.TotalSeconds.ToString("F1", Invariant);

    /// <summary>Quotes a field for CSV output when it contains a comma, quote, or newline.</summary>
    public static string CsvEscape(string value)
    {
        if (value.IndexOfAny([',', '"', '\n', '\r']) < 0)
            return value;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
