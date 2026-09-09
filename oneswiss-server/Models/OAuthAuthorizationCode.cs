namespace OneSwiss.Server.Models;

public class OAuthAuthorizationCode : DatabaseObject
{
    public string CodeHash { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public Guid UserId { get; set; }
    public string RedirectUri { get; set; } = string.Empty;
    public string CodeChallenge { get; set; } = string.Empty;
    public string CodeChallengeMethod { get; set; } = string.Empty;
    public string? Scope { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
}
