using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
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
    
    public virtual List<MaintenanceStep> Steps { get; set; } = [];
    public virtual List<InfoBaseMaintenanceTask> InfoBases { get; set; } = [];
    public virtual List<MaintenanceTaskLogItem> Logs { get; set; } = [];
}