using Microsoft.EntityFrameworkCore;
using MudBlazor;
using OneSwiss.Server.Models.MaintenanceTasks;

namespace OneSwiss.Server.Models;

public class InfoBase : DatabaseObject
{
    [Label("Внутренний идентификатор")]
    public string InfoBaseInternalId { get; set; } = string.Empty;
    [Label("Представление")]
    public string Name { get; set; } = string.Empty;
    [Label("Имя")]
    public string InfoBaseName { get; set; } = string.Empty;
    [Label("Адрес публикации")]
    public string PublishAddress { get; set; } = string.Empty;
    [Label("Учетные данные")]
    public Guid? CredentialsId { get; set; }
    [Label("Кластер")]
    public Guid ClusterId { get; set; }
    
    [DeleteBehavior(DeleteBehavior.SetNull)]
    public virtual Credentials? Credentials { get; set; }
    public virtual Cluster Cluster { get; set; } = null!;
    
    public virtual List<InfoBaseMaintenanceTask> MaintenanceTasks { get; set; } = [];
}