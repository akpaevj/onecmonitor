using Blazor.Diagrams.Core;
using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;

namespace OneSwiss.Server.Components.Pages.MaintenanceTasks;

public class StepPort : PortModel
{
    private Diagram _diagram;
    
    public StepPort(Diagram diagram, NodeModel parent, PortAlignment alignment = PortAlignment.Bottom, Point? position = null, Size? size = null) : base(parent, alignment, position, size)
    {
        _diagram = diagram;
    }

    public StepPort(Diagram diagram, string id, NodeModel parent, PortAlignment alignment = PortAlignment.Bottom, Point? position = null, Size? size = null) : base(id, parent, alignment, position, size)
    {
        _diagram = diagram;
    }

    public override bool CanAttachTo(ILinkable other)
    {
        if (other is StepPort port)
        {
            var canAttach = port.Links.Count == 0 && port.Parent.Id != Parent.Id &&
                            port.Alignment == PortAlignment.Left;
            
            // Найдем все циклы диаграммы, если есть хоть один, то отказываем в линке
            if (canAttach)
                return !_diagram.Nodes.Cast<StepNode>()
                    .Where(c => c.GetPort(PortAlignment.Left)!.Links.Count == 0)
                    .Any(HasLoop);
        }

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

                    if (anchor.Model is PortModel nextPort && Dfs(nextPort.Parent.Ports.Where(c => c.Id != nextPort.Id).ToList()))
                        return true;
                }
            }
            
            return false;
        }
    }
}