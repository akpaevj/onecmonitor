using System.ComponentModel.DataAnnotations;

namespace OneSwiss.Server.Models;

public class EventLogSettings : DatabaseObject
{
    public bool Enabled { get; set; }

    public Guid? DbmsId { get; set; }

    
    [MaxLength(100)]
    public string DatabaseName { get; set; } = string.Empty;

    
    [MaxLength(100)]
    public string Table { get; set; } = string.Empty;

    
    public Guid? CredentialsId { get; set; }
    
    
    public string InfoBaseNameRegex { get; set; } = string.Empty;
    public int DefaultTtl { get; set; } = 365;

    public Dbms? Dbms { get; set; }
    public Credentials? Credentials { get; set; }
}