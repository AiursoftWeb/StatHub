using Microsoft.Extensions.Logging;

namespace Aiursoft.StatHub.Client.Services;

public class ClientIdService(ILogger<ClientIdService> logger)
{
    public static readonly string AppDirectoryLocation = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "StatHubClient");

    public static readonly string ConfigFileName = "config.conf";

    public static readonly string ConfileFileLocation = Path.Combine(AppDirectoryLocation, ConfigFileName);

    public Task<string> GetConfigFileContent()
    {
        logger.LogTrace("Getting config file content...");
        if (!Directory.Exists(AppDirectoryLocation))
        {
            logger.LogTrace("The directory for config file {AppDirectory} was not found. Creating it...", AppDirectoryLocation);
            Directory.CreateDirectory(AppDirectoryLocation);
        }

        if (!File.Exists(ConfileFileLocation))
        {
            logger.LogTrace("The file for config {ConfileFileLocation} was not found. Creating it...", ConfileFileLocation);
            File.Create(ConfileFileLocation).Close();
        }

        logger.LogTrace("Reading config file: {ConfileFileLocation} ...", ConfileFileLocation);
        return File.ReadAllTextAsync(ConfileFileLocation);
    }

    private Task SetConfigFileContent(string newContent)
    {
        logger.LogTrace("Setting config file content...");
        if (!Directory.Exists(AppDirectoryLocation))
        {
            logger.LogTrace("The directory for config file {AppDirectory} was not found. Creating it...", AppDirectoryLocation);
            Directory.CreateDirectory(AppDirectoryLocation);
        }

        logger.LogTrace("Writing config file: {ConfileFileLocation} ...", ConfileFileLocation);
        return File.WriteAllTextAsync(ConfileFileLocation, newContent);
    }

    public async Task<string> GetClientId()
    {
        logger.LogTrace("Getting id...");
        var id = await GetConfigFileContent();

        if (string.IsNullOrWhiteSpace(id))
        {
            var newId = Guid.NewGuid().ToString("N");
            logger.LogTrace("The id from config file was empty. Setting it to default, which is {NewId}", newId);
            await SetConfigFileContent(newId);

            id = await GetConfigFileContent();
        }

        return id;
    }
}
