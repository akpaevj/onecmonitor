using Microsoft.EntityFrameworkCore;

namespace OneSwiss.Server.Models.MaintenanceTasks;

[PrimaryKey(nameof(InfoBaseId), nameof(MaintenanceTaskId))]
public class InfoBaseMaintenanceTask
{
    public Guid InfoBaseId { get; set; }
    public InfoBase InfoBase { get; set; } = null!;
    public Guid MaintenanceTaskId { get; set; }
    public MaintenanceTask MaintenanceTask { get; set; } = null!;

    protected bool Equals(InfoBaseMaintenanceTask other)
    {
        return InfoBaseId.Equals(other.InfoBaseId) && MaintenanceTaskId.Equals(other.MaintenanceTaskId);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        return obj.GetType() == GetType() && Equals((InfoBaseMaintenanceTask)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(InfoBaseId, MaintenanceTaskId);
    }
}