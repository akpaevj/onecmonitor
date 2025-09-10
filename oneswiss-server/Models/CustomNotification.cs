using System.ComponentModel.DataAnnotations;

namespace OneSwiss.Server.Models;

public class CustomNotification : DatabaseObject
{
    [MaxLength(30)] public string Key { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = true)] public string Description { get; set; } = string.Empty;
}