using System.ComponentModel.DataAnnotations;

namespace OnecMonitor.Server.Models;

public class TechLogSettings : DatabaseObject
{
    public bool Enabled { get; set; }
    [MaxLength(100)]
    public string ClickHouseHost { get; set; } = string.Empty;
    public int ClickHousePort { get; set; }
    [MaxLength(100)]
    public string ClickHouseDatabase { get; set; } = string.Empty;
    [MaxLength(100)]
    public string ClickHouseUser { get; set; } = string.Empty;
    [MaxLength(100)] 
    public string ClickHousePassword { get; set; } = string.Empty;
}