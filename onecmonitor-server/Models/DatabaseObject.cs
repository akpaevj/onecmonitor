namespace OnecMonitor.Server.Models;

public abstract class DatabaseObject
{
    public Guid Id { get; set; }

    private bool Equals(DatabaseObject other)
    {
        return Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((DatabaseObject)obj);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
    
}