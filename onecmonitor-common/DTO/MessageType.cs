namespace OnecMonitor.Common.DTO
{
    public enum MessageType
    {
        // System messages
        Error,
        Ok,
        
        // Client messages
        AgentInfo,
        TechLogSeancesRequest,
        LastFilePositionRequest,
        TechLogEventContent,
        InstalledPlatforms,
        RagentServices,
        RasServices,
        ClustersResponse,
        InfoBasesResponse,
        UpdateInfoBaseTaskLog,
        UpdateInfoBasesTaskRequest,
        SettingsRequest,
        
        MaintenanceStepNodeLog,
        
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
        UpdateSettingsRequest,
        Settings,
        
        MaintenanceTask,
    }
}
