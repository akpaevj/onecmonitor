using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OneSwiss.Server.Models;

public abstract class DatabaseObject : IHasId
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
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