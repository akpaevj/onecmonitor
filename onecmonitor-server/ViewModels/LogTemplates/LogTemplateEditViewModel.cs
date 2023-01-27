using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.ViewModels.Log
{
    public class LogTemplateEditViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
