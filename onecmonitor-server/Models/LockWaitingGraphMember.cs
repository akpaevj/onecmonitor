namespace OnecMonitor.Server.Models
{
    public class LockWaitingGraphMember
    {
        public TjEvent? Event { get; set; } = null;
        public DateTime LockAffectEndDateTime { get; set; } = DateTime.MinValue;
        public List<Guid> DirectCulprits { get; set; } = new();
        public List<Guid> IndirectCulprits { get; set; } = new();
        public LockWaitingTimelineMemberType MemberType { get; set; }

        public bool Unknown { get; set; } = false;
        /// <summary>
        /// it might be filled if this member is an unknown member, but we know t:connectID property value
        /// </summary>
        public int TConnectId { get; set; }

        public LockWaitingGraphMember()
        {
            Unknown = true;
        }

        public LockWaitingGraphMember(int tConnectId) : base()
        {
            TConnectId = tConnectId;
        }

        public LockWaitingGraphMember(TjEvent tjEvent)
        {
            Event = tjEvent;
        }
    }
}
