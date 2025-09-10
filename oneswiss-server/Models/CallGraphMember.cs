using OneSwiss.Common.Models;

namespace OneSwiss.Server.Models;

public class CallGraphMember
{
    public CallGraphMember(TjEvent tjEvent)
    {
        Event = tjEvent;
    }

    public TjEvent? Event { get; set; }

    public override string ToString()
    {
        return Event?.EventName ?? "Unknown event";
    }
}