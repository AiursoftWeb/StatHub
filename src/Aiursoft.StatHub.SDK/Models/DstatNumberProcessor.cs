using System.Globalization;

namespace Aiursoft.StatHub.SDK.Models;

public static class DstatNumberProcessor
{
    /// <summary>
    /// Parses a data size string (e.g., "100", "1k", "1.5M", "1G") into bytes.
    /// Supports B, k, M, G, T suffixes.
    /// </summary>
    /// <param name="number">The string to parse.</param>
    /// <returns>The size in bytes.</returns>
    public static long ParseDataSize(string number)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            return 0;
        }

        var span = number.AsSpan().Trim();
        if (span.IsEmpty)
        {
            return 0;
        }

        var lastChar = span[^1];
        if (char.IsDigit(lastChar))
        {
            return long.TryParse(span, CultureInfo.InvariantCulture, out var result) ? result : 0;
        }

        var numericPart = span[..^1];
        if (!double.TryParse(numericPart, CultureInfo.InvariantCulture, out var value))
        {
            return 0;
        }

        return lastChar switch
        {
            'B' => (long)value,
            'k' => (long)(value * 1024),
            'M' => (long)(value * 1024 * 1024),
            'G' => (long)(value * 1024 * 1024 * 1024),
            'T' => (long)(value * 1024 * 1024 * 1024 * 1024),
            _ => (long)value
        };
    }
}
