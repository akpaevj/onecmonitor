using Microsoft.EntityFrameworkCore;

namespace OneSwiss.Server.Models;

[Index(nameof(Hash))]
public class ErrorReport : DatabaseObject
{
    public DateTime CreatedAt { get; set; }
    public string Report { get; set; }
    public byte[] Screenshot { get; set; } = null!;
    public byte[] Hash { get; set; }
}