namespace Aiursoft.StatHub.SDK.Models;

public class SshKeyInfo
{
    public string FilePath { get; set; } = null!;
    public string Preview { get; set; } = null!;
    public string Owner { get; set; } = null!;
    public DateTime LastModified { get; set; }
    public DateTime LastAccessed { get; set; }
    public DateTime Created { get; set; }
}
