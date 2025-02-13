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
        RagentServices,
        RasServices,
        ClustersResponse,
        InfoBasesResponse,
        UpdateInfoBaseTaskResult,
        UpdateInfoBasesTaskRequest,
        
        // Server messages
        LastFilePosition,
        TechLogSeances,
        UpdateTechLogSeancesRequest,
        InstalledPlatformsRequest,
        RagentServicesRequest,
        RasServicesRequest,
        ClustersRequest,
        InfoBasesRequest,
        UpdateInfoBasesRequest,
        UpdateInfoBasesTask,
        UpdateSettingsRequest
    }
}
