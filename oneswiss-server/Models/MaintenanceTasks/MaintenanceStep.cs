using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MudBlazor;
using OneSwiss.Common.Models.MaintenanceTasks;

namespace OneSwiss.Server.Models.MaintenanceTasks;

public class MaintenanceStep : DatabaseObject
{
    [Label("Задача")]
    public Guid MaintenanceTaskId { get; set; }
    
    [Label("Тип шага")]
    public MaintenanceStepKind Kind { get; set; }
    [Label("Тип ноды")]
    public MaintenanceStepNodeKind NodeKind { get; set; }
    
    [Label("Предыдущий шаг")]
    public Guid? PreviousStepId { get; set; }
    [Label("Левый шаг")]
    public Guid? LeftStepId { get; set; }
    [Label("Правый шаг")]
    public Guid? RightStepId { get; set; }
    
    [Label("Код доступа")]
    [MaxLength(20)] 
    public string AccessCode { get; set; } = string.Empty;
    [Label("Сообщение")]
    [MaxLength(200)] 
    public string Message { get; set; } = string.Empty;
    [Label("Аргументы командной строки")]
    [MaxLength(1000)] 
    public string CommandLineArguments { get; set; } = string.Empty;
    [Label("Файл")]
    public Guid? FileId { get; set; }
    
    [ForeignKey(nameof(FileId))]
    public File? File { get; set; }
    
    [Label("Имя расширения")]
    [MaxLength(200)] 
    public string ExtensionName { get; set; } = string.Empty;
    
    // Вспомогательные свойства для UI
    public double PositionX { get; set; }
    public double PositionY { get; set; }

    [ForeignKey(nameof(MaintenanceTaskId))]
    public MaintenanceTask MaintenanceTask { get; set; } = null!;
    public virtual List<MaintenanceStepLogItem> Logs { get; set; } = [];
}