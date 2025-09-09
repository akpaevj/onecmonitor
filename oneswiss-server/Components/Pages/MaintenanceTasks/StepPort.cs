using Blazor.Diagrams.Core;
using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;

namespace OneSwiss.Server.Components.Pages.MaintenanceTasks;

public class StepPort(
    Diagram diagram,
    StepPortType type,
    NodeModel parent,
    PortAlignment alignment = PortAlignment.Bottom,
    Point? position = null,
    Size? size = null)
    : PortModel(parent, alignment, position, size)
{
    public StepPortType Type { get; } = type;
    public bool IsIn => Type == StepPortType.In;
    public bool IsSuccess => Type == StepPortType.Success;
    public bool IsFailure => Type == StepPortType.Failure;
    public new StepNode Parent => (base.Parent as StepNode)!;

    public override bool CanAttachTo(ILinkable other)
    {
        if (other is not StepPort port)
            return false;

        var canAttach = port.Links.Count == 0 && port.Parent.Id != Parent.Id && port.Type == StepPortType.In;

        // Найдем все циклы диаграммы, если есть хоть один, то отказываем в линке
        if (canAttach)
            return !diagram.Nodes.Cast<StepNode>()
                .Where(c => c.GetInPort()!.Links.Count == 0)
                .Any(HasLoop);

        return false;
    }

    private static bool HasLoop(NodeModel node)
    {
        var visited = new HashSet<string>();

        return Dfs(node.Ports.ToList());

        bool Dfs(List<PortModel> ports)
        {
            foreach (var port in ports)
            {
                if (!visited.Add(port.Id))
                    return true;

                foreach (var link in port.Links)
                {
                    if (link.Target is not SinglePortAnchor anchor)
                        continue;

                    if (anchor.Model is PortModel nextPort &&
                        Dfs(nextPort.Parent.Ports.Where(c => c.Id != nextPort.Id).ToList()))
                        return true;
                }
            }

            return false;
        }
    }
}