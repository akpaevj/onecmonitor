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
    [Label("Из хранилища")]
    public bool FromConfigRepository { get; set; }
    [Label("Хранилище")]
    public Guid? ConfigurationRepositoryId { get; set; }
    
    [ForeignKey(nameof(ConfigurationRepositoryId))]
    public ConfigurationRepository? ConfigurationRepository { get; set; }
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
    public virtual List<MaintenanceTaskLogItem> Logs { get; set; } = [];
    
    public bool IsConfigurationUpdating()
        => Kind is MaintenanceStepKind.LoadExtension
            or MaintenanceStepKind.LoadConfiguration;

    public bool NeedSpecifyConfigRepository()
    {
        return Kind is MaintenanceStepKind.LoadExtension
            or MaintenanceStepKind.LoadConfiguration && FromConfigRepository;
    }

    public bool NeedLoadFiles()
    {
        if (Kind is MaintenanceStepKind.LoadExtension
            or MaintenanceStepKind.LoadConfiguration)
            return !FromConfigRepository;
        
        return Kind is MaintenanceStepKind.ExecuteOneScript
            or MaintenanceStepKind.StartExternalDataProcessor
            or MaintenanceStepKind.UpdateConfiguration;
    }
    
    public bool NeeSpecifyAccessCode()
        => Kind is MaintenanceStepKind.LockConnections;
    
    public bool NeeSpecifyMessage()
        => Kind is MaintenanceStepKind.LockConnections;
    
    public bool NeeSpecifyExtensionName()
        => Kind is MaintenanceStepKind.LoadExtension
            or MaintenanceStepKind.DeleteExtension;
    
    public FileType AvailableFileType
        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        => Kind switch
        {
            MaintenanceStepKind.LoadExtension => FileType.Cfe,
            MaintenanceStepKind.UpdateConfiguration => FileType.Cfu,
            MaintenanceStepKind.LoadConfiguration => FileType.Cf,
            MaintenanceStepKind.StartExternalDataProcessor => FileType.Epf,
            MaintenanceStepKind.ExecuteOneScript => FileType.Ospx,
            _ => throw new ArgumentOutOfRangeException()
        };
}