namespace OnecMonitor.Server.ViewModels.TechLogSeances
{
    public class TechLogSeancesIndexViewModel
    {
        public bool Enabled { get; set; } = false;
        public int PageSize { get; set; } = 50;
        public int CurrentPage { get; set; } = 1;
        public int PagesCount { get; set; }
        public List<TechLogSeancesListItemViewModel> Seances { get; set; } = new();
        public bool RepositoryAvailable { get; set; }
    }
}
