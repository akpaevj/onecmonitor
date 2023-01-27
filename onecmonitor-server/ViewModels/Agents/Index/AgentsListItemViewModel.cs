namespace OnecMonitor.Server.ViewModels.Agents.Index
{
    public class AgentsListItemViewModel
    {
        public Guid Id { get; set; }
        public string InstanceName { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
    }
}