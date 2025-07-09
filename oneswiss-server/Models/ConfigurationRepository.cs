namespace OneSwiss.Server.Models;

public class ConfigurationRepository : DatabaseObject
{
    public string Name { get; set; }
    public string Address { get; set; }
    public Guid AgentId { get; set; }
    public Guid? CredentialsId { get; set; }
    
    public Agent Agent { get; set; }
    public Credentials? Credentials { get; set; }
    
    public List<ConfigurationRepositoryUser> Users { get; set; }
}