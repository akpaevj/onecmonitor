namespace OneSwiss.Server.Models;

public class GitSyncSettings : DatabaseObject
{
    public bool Enabled { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string LfsTrackers { get; set; } = string.Empty;
}