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
        ClusterDetailsResponse,
        InfoBasesResponse,
        InfoBaseDetailsResponse,
        V8SessionsResponse,
        V8ProcessesResponse,
        SettingsRequest,
        MaintenanceStepNodeLog,
        V8FileRequest,
        SystemInfoRequest,
        ConfigRepositoryDetailsRequest,
        EdtInstallationsRequest,
        
        // Server messages
        InstalledPlatformsRequest,
        RagentServicesRequest,
        RasServicesRequest,
        CrServerServicesRequest,
        ClustersRequest,
        ClusterDetailsRequest,
        ChangeClusterParametersRequest,
        ChangeInfoBaseParametersRequest,
        InfoBasesRequest,
        InfoBaseDetailsRequest,
        V8SessionsRequest,
        V8ProcessesRequest,
        Settings,
        MaintenanceTask,
        V8FileChunk,
        SystemInfo,
        ConfigRepositoryDetails,
        EdtInstallations
    }
}
