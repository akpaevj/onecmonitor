using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace OneSwiss.Server.Models;

public class Cluster : DatabaseObject
{
    [Label("Внутренний идентификатор")]
    public string ClusterInternalId { get; set; } = string.Empty;
    [Label("Имя")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    [Label("Хост")]
    [MaxLength(100)]
    public string Host { get; set; } = string.Empty;
    [Label("Порт")]
    public int Port { get; set; }
    [Label("Агент")]
    public Guid AgentId { get; set; }
    [Label("Учетные данные")]
    public Guid? CredentialsId { get; set; }
    
    public virtual Agent Agent { get; set; } = null!;
    [DeleteBehavior(DeleteBehavior.SetNull)]
    public virtual Credentials? Credentials { get; set; }
    public virtual List<InfoBase> InfoBases { get; set; } = [];
}