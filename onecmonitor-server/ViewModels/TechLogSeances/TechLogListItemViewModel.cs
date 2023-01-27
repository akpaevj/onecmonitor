using OnecMonitor.Server.Models;
using System.Security.Policy;

namespace OnecMonitor.Server.ViewModels.TechLogSeances
{
    public class TechLogListItemViewModel
    {
        public TjEvent TjEvent { get; set; }
        public string AdditionalCss { get; set; } = string.Empty;
        public List<TjEventAction> AvailableActions { get; } = new();

        public TechLogListItemViewModel(TjEvent tjEvent)
        {
            TjEvent = tjEvent;

            if (TjEvent.EventName == "TLOCK" && tjEvent.WaitConnections.Length > 0)
            {
                AdditionalCss = "om-warning";
                AvailableActions.Add(TjEventAction.ShowLockWaitingTimeline);
                AvailableActions.Add(TjEventAction.ShowLockWaitingGraph);
            }
            else if (TjEvent.EventName == "TTIMEOUT")
            {
                AdditionalCss = "om-danger";
                AvailableActions.Add(TjEventAction.ShowLockWaitingTimeline);
                AvailableActions.Add(TjEventAction.ShowLockWaitingGraph);
            }
            else if (TjEvent.EventName == "TDEADLOCK")
            {
                AdditionalCss = "om-danger";
                AvailableActions.Add(TjEventAction.ShowLockWaitingTimeline);
                AvailableActions.Add(TjEventAction.ShowLockWaitingGraph);
            }
            else if (TjEvent.EventName == "CALL")
                AvailableActions.Add(TjEventAction.CallTimeline);
            else if (TjEvent.EventName == "SCALL")
                AvailableActions.Add(TjEventAction.CallTimeline);
        }
    }
}
