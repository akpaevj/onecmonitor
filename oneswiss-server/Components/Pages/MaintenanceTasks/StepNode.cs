using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using OneSwiss.Common.Models.MaintenanceTasks;
using OneSwiss.Server.Models.MaintenanceTasks;

namespace OneSwiss.Server.Components.Pages.MaintenanceTasks;

public class StepNode : NodeModel
{
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

    public MaintenanceStep Step { get; }

    public StepPort? GetPort(StepPortType type)
    {
        var port = Ports.FirstOrDefault(p => p is StepPort port && port.Type == type);
        return port as StepPort;
    }

    public StepPort? GetInPort()
    {
        return GetPort(StepPortType.In);
    }

    public StepPort? GetSuccessPort()
    {
        return GetPort(StepPortType.Success);
    }

    public StepPort? GetFailurePort()
    {
        return GetPort(StepPortType.Failure);
    }
}