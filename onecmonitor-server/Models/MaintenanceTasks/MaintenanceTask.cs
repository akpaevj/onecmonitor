using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnecMonitor.Server.Models.MaintenanceTasks;

public class MaintenanceTask : DatabaseObject
{
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; } = DateTime.MinValue;
    public bool IsFaulted { get; set; } = false;
    public DateTime FinishDateTime { get; set; } = DateTime.MinValue;
    
    public virtual List<MaintenanceStep> Steps { get; set; } = [];
    public virtual List<InfoBase> InfoBases { get; set; } = [];
}