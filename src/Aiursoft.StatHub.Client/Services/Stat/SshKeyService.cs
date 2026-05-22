using Aiursoft.Canon;
using Aiursoft.StatHub.SDK.Models;

namespace Aiursoft.StatHub.Client.Services.Stat;

public class SshKeyService(CacheService cacheService)
{
    public Task<List<SshKeyInfo>> GetSshKeysAsync()
    {
        return cacheService.RunWithCache("ssh-keys", async () =>
        {
            var keys = new List<SshKeyInfo>();
            var passwd = await File.ReadAllTextAsync("/etc/passwd");
            foreach (var line in passwd.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split(':');
                if (parts.Length < 6) continue;
                var username = parts[0];
                var home = parts[5];
                if (string.IsNullOrWhiteSpace(home) || !Directory.Exists(home)) continue;

                var authKeysDir = Path.Combine(home, ".ssh");
                if (!Directory.Exists(authKeysDir)) continue;

                var authorizedKeys = Path.Combine(authKeysDir, "authorized_keys");
                if (!File.Exists(authorizedKeys)) continue;

                var lines = await File.ReadAllLinesAsync(authorizedKeys);
                var fileInfo2 = new FileInfo(authorizedKeys);
                foreach (var keyLine in lines.Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith('#')))
                {
                    var trimmed = keyLine.Trim();
                    keys.Add(new SshKeyInfo
                    {
                        FilePath = authorizedKeys,
                        Preview = trimmed.Length > 30 ? trimmed[..30] : trimmed,
                        Owner = username,
                        LastModified = fileInfo2.LastWriteTimeUtc,
                        LastAccessed = fileInfo2.LastAccessTimeUtc,
                        Created = fileInfo2.CreationTimeUtc
                    });
                }
            }
            return keys;
        }, cachedMinutes: _ => TimeSpan.FromHours(1));
    }
}
