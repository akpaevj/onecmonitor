using AutoMapper;
using OneSwiss.Common.DTO;
using OneSwiss.Common.DTO.MaintenanceTasks;
using OneSwiss.Server.Models;
using OneSwiss.Server.Models.MaintenanceTasks;
using OneSwiss.V8.Platform.RemoteAdministration;
using File = OneSwiss.Server.Models.File;

namespace OneSwiss.Server.AutoMapper;

public class DtoProfile : Profile
{
    public DtoProfile()
    {
        CreateMap<Agent, AgentDto>().ReverseMap();

        CreateMap<Credentials, CredentialsDto>().ReverseMap();

        CreateMap<Cluster, ClusterDto>().ReverseMap();
        CreateMap<V8ClusterDetails, V8ClusterDetails>().ReverseMap();

        CreateMap<Dbms, DbmsDto>().ReverseMap();

        CreateMap<GitRepository, GitRepositoryDto>().ReverseMap();
        CreateMap<GitSyncTask, GitSyncTaskDto>().ReverseMap();
        CreateMap<GitSyncTaskItem, GitSyncTaskItemDto>().ReverseMap();
        CreateMap<GitSyncSettings, GitSyncSettingsDto>().ReverseMap();

        CreateMap<EventLogSettings, EventLogSettingsDto>().ReverseMap();
        CreateMap<EventLogExportItem, EventLogExportItemDto>().ReverseMap();

        CreateMap<TechLogSettings, TechLogSettingsDto>().ReverseMap();

        CreateMap<V8Cluster, Cluster>()
            .ForMember(c => c.Id, opt => opt.Ignore())
            .ForMember(c => c.ClusterInternalId, opt => opt.MapFrom(src => src.Id))
            .ReverseMap();

        CreateMap<ConfigurationRepository, ConfigurationRepositoryDto>().ReverseMap();
        CreateMap<ConfigurationRepositoryUser, ConfigRepositoryUserDto>().ReverseMap();

        CreateMap<File, FileDto>()
            .ForMember(c => c.FileExtension, opt => opt.MapFrom(src => Path.GetExtension(src.DataPath)))
            .AfterMap<FileMappingAction>();

        CreateMap<InfoBase, InfoBaseDto>().ReverseMap();
        CreateMap<V8InfoBaseDetails, V8InfoBaseDetails>().ReverseMap();

        CreateMap<V8InfoBase, InfoBase>()
            .ForMember(c => c.Id, opt => opt.Ignore())
            .ForMember(c => c.InfoBaseInternalId, opt => opt.MapFrom(src => src.Id))
            .ForMember(c => c.InfoBaseName, opt => opt.MapFrom(src => src.Name));

        CreateMap<CopyInfoBaseStep, CopyInfoBaseStepDto>().ReverseMap();
        CreateMap<LoadConfigurationStep, LoadConfigurationStepDto>().ReverseMap();
        CreateMap<LoadExtensionStep, LoadExtensionStepDto>().ReverseMap();
        CreateMap<UpdateConfigurationStep, UpdateConfigurationStepDto>().ReverseMap();
        CreateMap<ExecuteOneScriptStep, ExecuteOneScriptStepDto>().ReverseMap();
        CreateMap<StartExternalDataProcessorStep, StartExternalDataProcessorStepDto>().ReverseMap();
        CreateMap<LockConnectionsStep, LockConnectionsStepDto>().ReverseMap();
        CreateMap<DeleteExtensionStep, DeleteExtensionStepDto>().ReverseMap();
        CreateMap<MaintenanceStep, MaintenanceStepDto>().ReverseMap();

        CreateMap<MaintenanceTask, MaintenanceTaskDto>()
            .ReverseMap();
        CreateMap<MaintenanceTaskLogItem, MaintenanceTaskLogItemDto>().ReverseMap();
    }
}