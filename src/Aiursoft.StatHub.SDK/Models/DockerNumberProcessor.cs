using System.Globalization;

namespace Aiursoft.StatHub.SDK.Models;

public static class DockerNumberProcessor
{
    public static long ParseDockerSize(string size)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(size))
            {
                return 0;
            }
            
            if (size.Contains('/'))
            {
                size = size.Split('/')[0];
            }
            size = size.Trim().ToUpper();
            if (size.EndsWith("KIB")) return TryParseDataSize(size[..^3], 1024);
            if (size.EndsWith("MIB")) return TryParseDataSize(size[..^3], 1024 * 1024);
            if (size.EndsWith("GIB")) return TryParseDataSize(size[..^3], 1024 * 1024 * 1024);
            if (size.EndsWith("TIB")) return TryParseDataSize(size[..^3], 1024L * 1024 * 1024 * 1024);
            if (size.EndsWith("KB")) return TryParseDataSize(size[..^2], 1000);
            if (size.EndsWith("MB")) return TryParseDataSize(size[..^2], 1000 * 1000);
            if (size.EndsWith("GB")) return TryParseDataSize(size[..^2], 1000 * 1000 * 1000);
            if (size.EndsWith("B")) return TryParseDataSize(size[..^1], 1);
            
            if (double.TryParse(size, CultureInfo.InvariantCulture, out var result))
            {
                return (long)result;
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private static long TryParseDataSize(string value, long multiplier)
    {
        if (double.TryParse(value.Trim(), CultureInfo.InvariantCulture, out var result))
        {
            return (long)(result * multiplier);
        }
        return 0;
    }
}
