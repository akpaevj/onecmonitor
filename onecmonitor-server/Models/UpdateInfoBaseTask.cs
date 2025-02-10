using System.ComponentModel.DataAnnotations;

namespace OnecMonitor.Server.Models;

public class UpdateInfoBaseTask : DatabaseObject
{
    [MaxLength(150)]
    public string Description { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; } = DateTime.MinValue;
    
    public Guid ConfigurationId { get; set; }
    public V8Configuration? Configuration { get; set; }
    
    public List<V8Configuration> Extensions { get; set; } = [];
    public List<InfoBase> InfoBases { get; set; } = [];
    
    public List<UpdateInfoBaseTaskResult> Results { get; set; } = [];
}