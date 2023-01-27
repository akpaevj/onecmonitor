namespace OnecMonitor.Server.ViewModels.TechLogSeances
{
    public class TjEventAction
    {
        public string Name { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;

        public static TjEventAction CallTimeline => new()
        {
            Name = "Call timeline",
            Action = "CallTimeline"
        };

        public static TjEventAction ShowLockWaitingTimeline => new()
        {
            Name = "Lock waiting timeline",
            Action = "LockWaitingTimeline"
        };

        public static TjEventAction ShowLockWaitingGraph => new()
        {
            Name = "Lock waiting graph",
            Action = "LockWaitingGraph"
        };
    }
}
