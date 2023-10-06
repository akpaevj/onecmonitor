namespace OnecMonitor.Common.Models
{
    public class TechLogSeance
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public TechLogSeanceStartMode StartMode { get; set; } = TechLogSeanceStartMode.Immediately;
        public DateTime StartDateTime { get; set; } = DateTime.MinValue;
        public int Duration { get; set; } = 15;
        public DateTime FinishDateTime => DateTime.SpecifyKind(StartMode == TechLogSeanceStartMode.Monitor ? DateTime.MaxValue : StartDateTime.AddMinutes(Duration), DateTimeKind.Utc);

        public List<LogTemplate> ConnectedTemplates { get; set; } = new();
        public List<Agent> ConnectedAgents { get; set; } = new();
    }
}
