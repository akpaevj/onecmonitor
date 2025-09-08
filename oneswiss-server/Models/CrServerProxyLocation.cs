using System.ComponentModel.DataAnnotations.Schema;

namespace OneSwiss.Server.Models;

public class CrServerProxyLocation : DatabaseObject
{
    public Guid ConfigurationRepositoryId { get; set; }
    [ForeignKey(nameof(ConfigurationRepositoryId))]
    public ConfigurationRepository ConfigurationRepository { get; set; }
    public string Location { get; set; }
}