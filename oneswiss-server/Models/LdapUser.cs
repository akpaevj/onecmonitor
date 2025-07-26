namespace OneSwiss.Server.Models;

public class LdapUser
{
    public string? Sid { get; set; }
    public string? CommonName { get; set; }
    public string? SamAccountName { get; set; }
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}