namespace OnecMonitor.Server.ViewModels.LogTemplates
{
    public class LogTemplateEditViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
