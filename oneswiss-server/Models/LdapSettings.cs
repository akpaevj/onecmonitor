namespace OneSwiss.Server.Models;

public class LdapSettings : DatabaseObject
{
    public bool Enabled { get; set; }
    public string Server { get; set; } = string.Empty;
    public Guid? CredentialsId { get; set; }
    
    
    public Credentials? Credentials { get; set; }
}