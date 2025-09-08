using System.ComponentModel.DataAnnotations.Schema;

namespace OneSwiss.Server.Models;

public class GitSyncTask : DatabaseObject
{
    public Guid AgentId { get; set; }

    [ForeignKey(nameof(AgentId))] public Agent Agent { get; set; }

    public bool IsActive { get; set; }
    public string BranchName { get; set; }
    public Guid GitRepositoryId { get; set; }

    [ForeignKey(nameof(GitRepositoryId))] public GitRepository GitRepository { get; set; }

    public List<GitSyncTaskItem> Items { get; set; } = [];
}