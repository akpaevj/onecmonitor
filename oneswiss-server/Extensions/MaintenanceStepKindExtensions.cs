using OneSwiss.Common.Models.MaintenanceTasks;

namespace OneSwiss.Server.Extensions;

public static class MaintenanceStepKindExtensions
{
    public static string GetKindColor(this MaintenanceStepKind kind)
    {
        return kind switch
        {
            MaintenanceStepKind.LockConnections => "rgba(210, 132, 156, 0.7)",
            MaintenanceStepKind.CloseConnections => "rgba(214, 133, 129, 0.7)",
            MaintenanceStepKind.UnlockConnections => "rgba(211, 137, 108, 0.7)",
            MaintenanceStepKind.LoadExtension => "rgba(193, 150, 82, 0.7)",
            MaintenanceStepKind.DeleteExtension => "rgba(167, 162, 85, 0.7)",
            MaintenanceStepKind.UpdateConfiguration => "rgba(131, 173, 109, 0.7)",
            MaintenanceStepKind.LoadConfiguration => "rgba(86, 179, 148, 0.7)",
            MaintenanceStepKind.StartExternalDataProcessor => "rgba(69, 175, 196, 0.7)",
            MaintenanceStepKind.ExecuteOneScript => "rgba(119, 160, 220, 0.7)",
            MaintenanceStepKind.CopyInfoBase => "rgba(59, 160, 220, 0.7)",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }
}