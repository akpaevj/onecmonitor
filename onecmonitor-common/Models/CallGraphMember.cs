namespace OnecMonitor.Common.Models
{
    public class CallGraphMember
    {
        public TjEvent? Event { get; set; }

        public CallGraphMember(TjEvent tjEvent)
        {
            Event = tjEvent;
        }

        public override string ToString()
        {
            return Event?.EventName ?? "Unknown event";
        }
    }
}
