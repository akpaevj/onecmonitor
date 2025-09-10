using Microsoft.EntityFrameworkCore;

namespace OneSwiss.Server.Models;

public class ConfigurationRepository : DatabaseObject
{
    public Guid InternalId { get; set; }
    public string Name { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
    public bool Deleted { get; set; }
    public Guid AgentId { get; set; }
    public Guid? CredentialsId { get; set; }

    public Agent Agent { get; set; }

    [DeleteBehavior(DeleteBehavior.SetNull)]
    public Credentials? Credentials { get; set; }

    public List<ConfigurationRepositoryUser> Users { get; set; }

    public override string ToString()
    {
        return $"{Host}:{Port}/{Name}";
    }
}