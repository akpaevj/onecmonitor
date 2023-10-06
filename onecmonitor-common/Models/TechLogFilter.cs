namespace OnecMonitor.Common.Models
{
    public class TechLogFilter
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Filter { get; set; } = string.Empty;
    }
}
