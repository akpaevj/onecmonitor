using OneSTools.Common.Platform;

namespace OnecMonitor.Server.ViewModels.Agents;

public class AgentViewModel
{
    public Guid Id { get; set; }
    public string InstanceName { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public List<V8Platform> InstalledPlatforms { get; set; } = [];
}