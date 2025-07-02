namespace OneSwiss.Common.DTO
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
        V8FileRequest,
        
        // Server messages
        InstalledPlatformsRequest,
        RagentServicesRequest,
        RasServicesRequest,
        ClustersRequest,
        InfoBasesRequest,
        Settings,
        MaintenanceTask,
        V8FileChunk
    }
}
