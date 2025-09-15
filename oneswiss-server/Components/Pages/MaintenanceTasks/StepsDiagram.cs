using Blazor.Diagrams;
using Blazor.Diagrams.Options;

namespace OneSwiss.Server.Components.Pages.MaintenanceTasks;

public class StepsDiagram(BlazorDiagramOptions options) : BlazorDiagram(options)
{
    public List<StepNode> StepNodes => Nodes.Select(c => c as StepNode).ToList()!;
    public EventHandler? UpdateTask;
}