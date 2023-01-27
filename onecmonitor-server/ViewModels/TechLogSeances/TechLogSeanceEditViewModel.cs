using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnecMonitor.Server.Models;
using System.ComponentModel.DataAnnotations;

namespace OnecMonitor.Server.ViewModels.TechLogSeances
{
    public class TechLogSeanceEditViewModel
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public TechLogSeanceStartMode StartMode { get; set; } = TechLogSeanceStartMode.Immediately;
        public DateTime StartDateTime { get; set; } = DateTime.MinValue;
        public int Duration { get; set; } = 15;

        public List<(Guid Id, string Name)> ConnectedTemplates { get; set; } = new();
        [ValidateNever]
        public List<(Guid Id, string Name)> ConnectedAgents { get; set; } = new();
        [ValidateNever]
        public SelectList AllTemplates { get; set; }
        [ValidateNever]
        public SelectList AllAgents { get; set; }
    }
}
