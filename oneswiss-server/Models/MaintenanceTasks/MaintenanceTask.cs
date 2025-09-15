using System.ComponentModel.DataAnnotations;
using MudBlazor;

namespace OneSwiss.Server.Models.MaintenanceTasks;

public class MaintenanceTask : DatabaseObject
{
    [Label("Описание")] 
    [MaxLength(200)] 
    public string Description { get; set; } = string.Empty;

    [Label("Дата начала")] 
    public DateTime StartDateTime { get; set; } = DateTime.MinValue;

    [Label("Ошибка")] 
    public bool IsFaulted { get; set; } = false;

    [Label("Дата окончания")] 
    public DateTime FinishDateTime { get; set; } = DateTime.MinValue;

    [Label("Это шаблон")] 
    public bool IsTemplate { get; set; } = false;

    [Label("Это задача общего назначения")]
    public bool CommonDestination { get; set; } = false;
    public bool StartWhenDiscoverNewConfigVersion { get; set; }

    public virtual List<MaintenanceStep> Steps { get; set; } = [];
    public virtual List<Agent> Agents { get; set; } = [];
    public virtual List<InfoBase> InfoBases { get; set; } = [];
    public virtual List<MaintenanceTaskLogItem> Logs { get; set; } = [];
}