namespace OneSwiss.Server.Models;

public class OAuthClient : DatabaseObject
{
    public string? ClientName { get; set; }
    public string RedirectUrisJson { get; set; } = "[]";
    public DateTime CreatedAtUtc { get; set; }
}
