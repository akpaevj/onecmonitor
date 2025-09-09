using System.ComponentModel.DataAnnotations.Schema;

namespace OneSwiss.Server.Models;

public class GitRepository : DatabaseObject
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public Guid TokenId { get; set; }

    [ForeignKey(nameof(TokenId))] public Credentials Token { get; set; }
}