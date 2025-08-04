using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace OneSwiss.Server.Models.MaintenanceTasks;

[Owned]
public class LockConnectionsStep
{
    [MaxLength(20)]
    public string AccessCode { get; set; } = string.Empty;
    [MaxLength(200)] 
    public string Message { get; set; } = string.Empty;
}