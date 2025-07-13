namespace OneSwiss.Server.Models;

public class ErrorReport : DatabaseObject
{
    public DateTime CreatedAt { get; set; }
    public string Report { get; set; }
    public byte[] Screenshot { get; set; } = null!;
}