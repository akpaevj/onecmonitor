using AutoMapper;
using OneSwiss.Common.DTO;
using OneSwiss.Common.DTO.MaintenanceTasks;
using OneSwiss.Server.Models;
using OneSwiss.Server.Models.MaintenanceTasks;
using OneSwiss.Server.Services;
using OneSwiss.V8.Platform.RemoteAdministration;
using File = OneSwiss.Server.Models.File;

namespace OneSwiss.Server.AutoMapper;

public class DtoProfile : Profile
{
    public DtoProfile()
    {
        CreateMap<Credentials, CredentialsDto>().ReverseMap();

        CreateMap<Cluster, ClusterDto>().ReverseMap();

        CreateMap<Dbms, DbmsDto>().ReverseMap();

        CreateMap<EventLogSettings, EventLogSettingsDto>().ReverseMap();
        
        CreateMap<TechLogSettings, TechLogSettingsDto>().ReverseMap();

        CreateMap<V8Cluster, Cluster>()
            .ForMember(c => c.Id, opt => opt.Ignore())
            .ForMember(c => c.ClusterInternalId, opt => opt.MapFrom(src => src.Id))
            .ReverseMap();
        
        CreateMap<File, FileDto>()
            .ForMember(c => c.FileExtension, opt => opt.MapFrom(src => Path.GetExtension(src.DataPath)))
            .AfterMap<FileMappingAction>();

        CreateMap<InfoBase, InfoBaseDto>().ReverseMap();
        
        CreateMap<V8InfoBaseSummary, InfoBase>()
            .ForMember(c => c.Id, opt => opt.Ignore())
            .ForMember(c => c.InfoBaseInternalId, opt => opt.MapFrom(src => src.Id))
            .ForMember(c => c.InfoBaseName, opt => opt.MapFrom(src => src.Name));

        CreateMap<MaintenanceStep, MaintenanceStepDto>().ReverseMap();
        CreateMap<MaintenanceTask, MaintenanceTaskDto>().ReverseMap();
        CreateMap<MaintenanceStepLogItem, MaintenanceStepLogItemDto>().ReverseMap();
    }
}