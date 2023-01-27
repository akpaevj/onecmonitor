using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.ViewModels.TechLogSeances
{
    public class LockWaitingTimelineViewModel
    {
        public Dictionary<Guid, LockWaitingGraphMember> Graph { get; set; } = new();
    }
}
