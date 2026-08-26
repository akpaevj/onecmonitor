using System.ComponentModel.DataAnnotations;

namespace OneSwiss.Server.Models.MaintenanceTasks;

public class MaintenanceTask : DatabaseObject
{
    
    [MaxLength(200)] 
    public string Description { get; set; } = string.Empty;

    
    public DateTime StartDateTime { get; set; } = DateTime.MinValue;

    
    public bool IsFaulted { get; set; } = false;

    
    public DateTime FinishDateTime { get; set; } = DateTime.MinValue;

    
    public bool IsTemplate { get; set; } = false;

    
    public bool CommonDestination { get; set; } = false;
    public bool StartWhenDiscoverNewConfigVersion { get; set; }

    public virtual List<MaintenanceStep> Steps { get; set; } = [];
    public virtual List<Agent> Agents { get; set; } = [];
    public virtual List<InfoBase> InfoBases { get; set; } = [];
    public virtual List<MaintenanceTaskLogItem> Logs { get; set; } = [];
}