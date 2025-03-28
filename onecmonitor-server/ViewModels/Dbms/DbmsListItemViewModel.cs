using OnecMonitor.Common.Models;

namespace OnecMonitor.Server.ViewModels.Dbms;

public class DbmsListItemViewModel
{
    public Guid Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public DbmsType Type { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
}