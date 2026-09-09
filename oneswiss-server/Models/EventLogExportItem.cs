using System.ComponentModel.DataAnnotations.Schema;

namespace OneSwiss.Server.Models;

public class EventLogExportItem : DatabaseObject
{
    public bool IsActive { get; set; }
    public Guid InfoBaseId { get; set; }
    public int Ttl { get; set; }

    public bool ReduceSourceLog { get; set; }
    public int ReduceKeepDays { get; set; } = 30;
    public DateTime? LastReducedUpTo { get; set; }

    [ForeignKey(nameof(InfoBaseId))]
    public InfoBase InfoBase { get; set; }
}