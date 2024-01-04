namespace Aiursoft.StatHub.SDK.Models;

public static class DstatNumberProcessor
{
    public static long ParseDataSize(string number)
    {
        if (number.EndsWith("B"))
        {
            return long.Parse(number.Replace("B", ""));
        }
        if (number.EndsWith("k"))
        {
            return long.Parse(number.Replace("k", "")) * 1024;
        }
        if (number.EndsWith("M"))
        {
            return long.Parse(number.Replace("M", "")) * 1024 * 1024;
        }
        if (number.EndsWith("G"))
        {
            return long.Parse(number.Replace("G", "")) * 1024 * 1024 * 1024;
        }
        return long.Parse(number);
    }
}