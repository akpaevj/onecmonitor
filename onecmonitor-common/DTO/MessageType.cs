namespace OnecMonitor.Common.DTO
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
        V8Services,
        ClustersResponse,
        InfoBasesResponse,
        
        // Server messages
        LastFilePosition,
        TechLogSeances,
        InstalledPlatformsRequest,
        V8ServicesRequest,
        ClustersRequest,
        InfoBasesRequest
    }
}
