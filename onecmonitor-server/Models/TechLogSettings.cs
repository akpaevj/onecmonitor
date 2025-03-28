using System.ComponentModel.DataAnnotations;

namespace OnecMonitor.Server.Models;

public class TechLogSettings : DatabaseObject
{
    public bool Enabled { get; set; }
    public Guid? DbmsId { get; set; }
    [MaxLength(100)]
    public string DatabaseName { get; set; } = string.Empty;
    [MaxLength(100)]
    public string Table { get; set; } = string.Empty;
    public Guid? CredentialsId { get; set; }
    
    public Dbms? Dbms { get; set; }
    public Credentials? Credentials { get; set; }
}