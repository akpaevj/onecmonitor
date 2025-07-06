using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using OneSwiss.Common.Models.MaintenanceTasks;
using OneSwiss.Server.Models;
using OneSwiss.Server.Models.MaintenanceTasks;
using File = OneSwiss.Server.Models.File;

namespace OneSwiss.Server.Components.Pages.MaintenanceTasks;

public class StepNode : NodeModel
{
    public MaintenanceStep Step { get; set; }
    public List<File> Files { get; set; } = [];

    public StepNode(MaintenanceStepKind kind, Point? position = null) : base(position)
    {
        Step = new MaintenanceStep
        {
            Kind = kind
        };
    }
    
    public StepNode(MaintenanceStep step, Point? position = null) : base(position)
    {
        Step = step;
    }
}