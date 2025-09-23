using System.ComponentModel.DataAnnotations.Schema;

namespace OneSwiss.Server.Models;

public class EventLogExportItem : DatabaseObject
{
    public bool IsActive { get; set; }
    public Guid InfoBaseId { get; set; }
    public int Ttl { get; set; }
    
    [ForeignKey(nameof(InfoBaseId))]
    public InfoBase InfoBase { get; set; }
}