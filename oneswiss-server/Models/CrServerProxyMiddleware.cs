using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OneSwiss.Server.Models;

public class CrServerProxyMiddleware : DatabaseObject
{
    public bool DebugMode { get; set; }
    public string ExecutablePath { get; set; } = string.Empty;
    public Guid? FileId { get; set; }
    public Guid LocationId { get; set; }
    [ForeignKey(nameof(FileId))]
    public File? File { get; set; }
    public bool ConnectAll { get; set; }
    [ForeignKey(nameof(LocationId))]
    public CrServerProxyLocation Location { get; set; }

    public List<ConfigurationRepositoryMiddlewareArgument> Arguments { get; set; } = [];
}