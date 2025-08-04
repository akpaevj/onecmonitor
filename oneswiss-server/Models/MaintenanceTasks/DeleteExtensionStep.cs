using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace OneSwiss.Server.Models.MaintenanceTasks;

[Owned]
public class DeleteExtensionStep
{
    [MaxLength(200)]
    public string ExtensionName { get; set; } = string.Empty;
}