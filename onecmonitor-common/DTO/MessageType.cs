namespace OnecMonitor.Common.DTO
{
    public enum MessageType
    {
        // System messages
        Error,
        Ok,
        
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
        UpdateInfoBaseTaskResult,
        UpdateInfoBasesTaskRequest,
        
        // Server messages
        LastFilePosition,
        TechLogSeances,
        UpdateTechLogSeancesRequest,
        InstalledPlatformsRequest,
        V8ServicesRequest,
        ClustersRequest,
        InfoBasesRequest,
        UpdateInfoBasesRequest,
        UpdateInfoBasesTask
    }
}
