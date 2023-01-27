namespace OnecMonitor.Common.Models
{
    public enum MessageType
    {
        AgentInfo = 0,
        SubscribingForCommands,
        LastFilePositionRequest,
        LastFilePosition,
        TechLogSeancesRequest,
        TechLogSeances,
        TechLogEventContent
    }
}
