namespace OnecMonitor.Common.DTO
{
    public enum MessageType
    {
        // System messages
        Error,
        Ok,
        
        // Client messages
        AgentInfo,
        InstalledPlatforms,
        RagentServices,
        RasServices,
        ClustersResponse,
        InfoBasesResponse,
        SettingsRequest,
        MaintenanceStepNodeLog,
        
        // Server messages
        InstalledPlatformsRequest,
        RagentServicesRequest,
        RasServicesRequest,
        ClustersRequest,
        InfoBasesRequest,
        Settings,
        MaintenanceTask,
    }
}
