using OnecMonitor.Common.DTO;

namespace OnecMonitor.Server.Models
{
    public class TechLogSeance : DatabaseObject
    {
        public string Description { get; set; } = string.Empty;
        public TechLogSeanceStartMode StartMode { get; set; } = TechLogSeanceStartMode.Immediately;
        public DateTime StartDateTime { get; set; } = DateTime.MinValue;
        public int Duration { get; set; } = 15;
        public bool DirectSending { get; set; } = false;
        public DateTime FinishDateTime => DateTime.SpecifyKind(StartMode == TechLogSeanceStartMode.Monitor ? DateTime.MaxValue : StartDateTime.AddMinutes(Duration), DateTimeKind.Utc);

        public List<LogTemplate> Templates { get; set; } = new();
        public List<Agent> Agents { get; set; } = new();
    }
}
