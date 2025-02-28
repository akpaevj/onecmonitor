namespace OnecMonitor.Agent.Models;

public class AgentInstance
{
    public Guid Id { get; set; } = Guid.Empty;
    public string InstanceName { get; set; } = string.Empty;
}