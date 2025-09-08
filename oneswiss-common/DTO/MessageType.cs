namespace OneSwiss.Common.DTO;

public enum MessageType
{
    // System messages
    Error,
    Ok,
    DataStreamHeader,
    DataStreamChunk,

    // Client messages
    AgentInfo,
    InstalledPlatforms,
    RagentServices,
    RasServices,
    CrServerServices,
    Clusters,
    ClusterDetails,
    InfoBases,
    InfoBaseDetails,
    V8Sessions,
    V8Processes,
    SettingsRequest,
    MaintenanceStepNodeLog,
    FileRequest,
    SystemInfoRequest,
    ConfigRepositoryDetailsRequest,
    EdtInstallationsRequest,
    QueueCustomNotificationRequest,
    GitSyncTasksRequest,
    GitSyncTaskProcessorStopped,
    CrServerPlatformRequest,

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
    GitSyncTasks,
    MaintenanceTask,
    SystemInfo,
    ConfigRepositoryDetails,
    EdtInstallations,
    CloseV8SessionsRequest,
    V8Platform
}