using OneSwiss.Common.Models;

namespace OneSwiss.Server.Models;

public class LockWaitingGraphMember
{
    public LockWaitingGraphMember()
    {
        Unknown = true;
    }

    public LockWaitingGraphMember(int tConnectId)
    {
        TConnectId = tConnectId;
    }

    public LockWaitingGraphMember(TjEvent tjEvent)
    {
        Event = tjEvent;
    }

    public TjEvent? Event { get; set; }
    public DateTime LockAffectEndDateTime { get; set; } = DateTime.MinValue;
    public List<Guid> DirectCulprits { get; set; } = new();
    public List<Guid> IndirectCulprits { get; set; } = new();
    public LockWaitingTimelineMemberType MemberType { get; set; }

    public bool Unknown { get; set; }

    /// <summary>
    ///     it might be filled if this member is an unknown member, but we know t:connectID property value
    /// </summary>
    public int TConnectId { get; set; }
}