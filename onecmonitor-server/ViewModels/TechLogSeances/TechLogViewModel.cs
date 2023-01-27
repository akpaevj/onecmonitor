using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.ViewModels.TechLogSeances
{
    public class TechLogViewModel
    {
        public Guid SeanceId { get; set; }
        public List<TechLogFilter> TechLogFilters { get; set; } = new();
        public string? Filter { get; set; }
        public int PageSize { get; set; } = 50;
        public int CurrentPage { get; set; } = 1;
        public int PagesCount { get; set; }
        public List<TechLogListItemViewModel> TjEvents { get; set; } = new();
    }
}
