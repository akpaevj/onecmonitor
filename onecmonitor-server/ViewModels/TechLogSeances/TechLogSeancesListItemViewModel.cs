using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.ViewModels.TechLogSeances
{
    public class TechLogSeancesListItemViewModel
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public TechLogSeanceStartMode StartMode { get; set; } = TechLogSeanceStartMode.Immediately;
        public DateTime StartDateTime { get; set; } = DateTime.MinValue;
        public int Duration { get; set; } = 0;
    }
}
