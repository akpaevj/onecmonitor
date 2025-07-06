using OneSwiss.Common.Models;

namespace OnecMonitor.Server.ViewModels.TechLogSeances
{
    public class TechLogListItemViewModel
    {
        public TjEvent TjEvent { get; set; }
        public string AdditionalCss { get; set; } = string.Empty;
        public List<TjEventAction> AvailableActions { get; } = [];

        public TechLogListItemViewModel(TjEvent tjEvent)
        {
            TjEvent = tjEvent;

            switch (TjEvent.EventName)
            {
                case "TLOCK" when tjEvent.WaitConnections.Length > 0:
                    AdditionalCss = "om-warning";
                    AvailableActions.Add(TjEventAction.ShowLockWaitingTimeline);
                    AvailableActions.Add(TjEventAction.ShowLockWaitingGraph);
                    break;
                case "TTIMEOUT":
                case "TDEADLOCK":
                    AdditionalCss = "om-danger";
                    AvailableActions.Add(TjEventAction.ShowLockWaitingTimeline);
                    AvailableActions.Add(TjEventAction.ShowLockWaitingGraph);
                    break;
                case "CALL":
                case "SCALL":
                    AvailableActions.Add(TjEventAction.CallTimeline);
                    break;
            }
        }
    }
}
