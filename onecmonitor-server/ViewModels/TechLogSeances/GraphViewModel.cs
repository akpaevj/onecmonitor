using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.ViewModels.TechLogSeances
{
    public class LockWaitingGraphViewModel
    {
        public Dictionary<Guid, LockWaitingGraphMember> Graph { get; set; } = new();
    }
}
