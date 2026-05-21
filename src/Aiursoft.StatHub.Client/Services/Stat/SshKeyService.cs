using Aiursoft.Canon;

namespace Aiursoft.StatHub.Client.Services.Stat;

public class SshKeyService(CacheService cacheService)
{
    public Task<int> GetSshKeyCountAsync()
    {
        return cacheService.RunWithCache("ssh-key-count", async () =>
        {
            var count = 0;
            var passwd = await File.ReadAllTextAsync("/etc/passwd");
            foreach (var line in passwd.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split(':');
                if (parts.Length < 6) continue;
                var home = parts[5];
                if (string.IsNullOrWhiteSpace(home) || !Directory.Exists(home)) continue;

                var authorizedKeys = Path.Combine(home, ".ssh", "authorized_keys");
                if (!File.Exists(authorizedKeys)) continue;

                var lines = await File.ReadAllLinesAsync(authorizedKeys);
                count += lines.Count(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith('#'));
            }
            return count;
        }, cachedMinutes: _ => TimeSpan.FromHours(1));
    }
}
