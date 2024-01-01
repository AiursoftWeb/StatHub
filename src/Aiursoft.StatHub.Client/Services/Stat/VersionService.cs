using System.Reflection;

namespace Aiursoft.StatHub.Client.Services.Stat;

public class VersionService
{
    public string GetAppVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version!.ToString();
    }
}