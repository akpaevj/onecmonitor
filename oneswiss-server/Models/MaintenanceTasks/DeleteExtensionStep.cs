using System.ComponentModel.DataAnnotations;

namespace OneSwiss.Server.Models.MaintenanceTasks;

public class DeleteExtensionStep : DatabaseObject
{
    [MaxLength(200)]
    public string ExtensionName { get; set; } = string.Empty;
}