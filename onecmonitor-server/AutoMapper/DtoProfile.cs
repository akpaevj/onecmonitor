using AutoMapper;
using OnecMonitor.Common.DTO;
using OnecMonitor.Common.DTO.MaintenanceTasks;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.Models.MaintenanceTasks;
using OneSTools.Common.Platform.RemoteAdministration;

namespace OnecMonitor.Server.AutoMapper;

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
        
        CreateMap<V8File, V8FileDto>()
            .ForMember(c => c.Length, opt => opt.MapFrom(src => new FileInfo(src.DataPath).Length))
            .ForMember(c => c.FileExtension, opt => opt.MapFrom(src => Path.GetExtension(src.DataPath)));

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