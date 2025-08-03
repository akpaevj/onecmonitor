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

    public StepNode(MaintenanceStepKind kind, Point? position = null) : base(position)
    {
        Step = new MaintenanceStep
        {
            Kind = kind
        };

        switch (Step.Kind)
        {
            case MaintenanceStepKind.DeleteExtension:
                Step.DeleteExtensionStep = new DeleteExtensionStep
                {
                    Id = Guid.NewGuid()
                };
                break;
            case MaintenanceStepKind.ExecuteOneScript:
                Step.ExecuteOneScriptStep = new ExecuteOneScriptStep
                {
                    Id = Guid.NewGuid()
                };
                break;
            case MaintenanceStepKind.LockConnections:
                Step.LockConnectionsStep = new LockConnectionsStep
                {
                    Id = Guid.NewGuid()
                };
                break;
            case MaintenanceStepKind.LoadExtension:
                Step.LoadExtensionStep = new LoadExtensionStep
                {
                    Id = Guid.NewGuid()
                };
                break;
            case MaintenanceStepKind.UpdateConfiguration:
                Step.UpdateConfigurationStep = new UpdateConfigurationStep
                {
                    Id = Guid.NewGuid()
                };
                break;
            case MaintenanceStepKind.LoadConfiguration:
                Step.LoadConfigurationStep = new LoadConfigurationStep
                {
                    Id = Guid.NewGuid()
                };
                break;
            case MaintenanceStepKind.StartExternalDataProcessor:
                Step.StartExternalDataProcessorStep = new StartExternalDataProcessorStep
                {
                    Id = Guid.NewGuid()
                };
                break;
            case MaintenanceStepKind.CopyInfoBase:
                Step.CopyInfoBaseStep = new CopyInfoBaseStep
                {
                    Id = Guid.NewGuid()
                };
                break;
            case MaintenanceStepKind.CloseConnections:
            case MaintenanceStepKind.UnlockConnections:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    public StepNode(MaintenanceStep step, Point? position = null) : base(position)
    {
        Step = step;
    }
}