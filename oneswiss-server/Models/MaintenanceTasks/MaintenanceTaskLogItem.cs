using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace OneSwiss.Server.Models.MaintenanceTasks;

public class MaintenanceTaskLogItem : DatabaseObject
{
    [DataType(DataType.Date)] public DateTime TimeStamp { get; set; }

    public bool IsError { get; set; }
    public bool IsFinish { get; set; }

    [MaxLength(200)] public string Message { get; set; } = string.Empty;

    public Guid? InfoBaseId { get; set; }

    [DeleteBehavior(DeleteBehavior.Cascade)]
    public InfoBase? InfoBase { get; set; }

    public Guid? StepId { get; set; }

    [DeleteBehavior(DeleteBehavior.Cascade)]
    public MaintenanceStep? Step { get; set; }

    public Guid? TaskId { get; set; }

    [DeleteBehavior(DeleteBehavior.Cascade)]
    public MaintenanceTask? Task { get; set; }
}