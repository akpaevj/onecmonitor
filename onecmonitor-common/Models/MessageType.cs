namespace OnecMonitor.Common.Models
{
    public enum MessageType
    {
        // System messages
        Error,
        
        // Client messages
        AgentInfo,
        SubscribingForCommands,
        TechLogSeancesRequest,
        LastFilePositionRequest,
        TechLogEventContent,
        InstalledPlatforms,
        
        // Server messages
        LastFilePosition,
        TechLogSeances,
        InstalledPlatformsRequest
    }
}
