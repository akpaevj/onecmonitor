namespace OneSwiss.Server.Models;

public class ConfigurationRepositoryUser : DatabaseObject
{
    public string User { get; set; }
    public string GitUser { get; set; }
    public bool Deleted { get; set; }
    public Guid RepositoryId { get; set; }
    
    public ConfigurationRepository Repository { get; set; }
}