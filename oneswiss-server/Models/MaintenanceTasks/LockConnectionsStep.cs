using System.ComponentModel.DataAnnotations;
using MudBlazor;

namespace OneSwiss.Server.Models.MaintenanceTasks;

public class LockConnectionsStep : DatabaseObject
{
    [MaxLength(20)]
    public string AccessCode { get; set; } = string.Empty;
    [MaxLength(200)] 
    public string Message { get; set; } = string.Empty;
}