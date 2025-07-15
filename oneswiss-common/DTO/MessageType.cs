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
        CrServerServices,
        ClustersResponse,
        InfoBasesResponse,
        SettingsRequest,
        MaintenanceStepNodeLog,
        V8FileRequest,
        SystemInfoRequest,
        ConfigRepositoryDetailsRequest,
        
        // Server messages
        InstalledPlatformsRequest,
        RagentServicesRequest,
        RasServicesRequest,
        CrServerServicesRequest,
        ClustersRequest,
        InfoBasesRequest,
        Settings,
        MaintenanceTask,
        V8FileChunk,
        SystemInfo,
        ConfigRepositoryDetails
    }
}
