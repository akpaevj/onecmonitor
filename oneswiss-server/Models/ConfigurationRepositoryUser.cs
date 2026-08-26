namespace OneSwiss.Server.Models;

public class ConfigurationRepositoryUser : DatabaseObject
{
    public Guid InternalId { get; set; }
    public string Name { get; set; }
    public bool Deleted { get; set; }
    public Guid RepositoryId { get; set; }

    public ConfigurationRepository Repository { get; set; }
}