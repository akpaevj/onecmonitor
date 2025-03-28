using System.ComponentModel.DataAnnotations;
using OnecMonitor.Common.Models;

namespace OnecMonitor.Server.Models;

public class Dbms : DatabaseObject
{
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    public DbmsType Type { get; set; }
    [MaxLength(100)]
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
}