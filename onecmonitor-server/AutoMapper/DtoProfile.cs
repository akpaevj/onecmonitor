using AutoMapper;
using OnecMonitor.Common.DTO;
using OnecMonitor.Common.DTO.MaintenanceTasks;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.Models.MaintenanceTasks;

namespace OnecMonitor.Server.AutoMapper;

public class DtoProfile : Profile
{
    public DtoProfile()
    {
        CreateMap<Credentials, CredentialsDto>().ReverseMap();
        
        CreateMap<Cluster, ClusterDto>()
            .ForMember(c => c.Id, opt => opt.MapFrom(src => src.ClusterInternalId))
            .ReverseMap();
        
        CreateMap<V8File, V8FileDto>()
            .ForMember(c => c.Data, opt => opt.MapFrom(src => File.ReadAllBytes(src.DataPath)))
            .ForMember(c => c.FileExtension, opt => opt.MapFrom(src => Path.GetExtension(src.DataPath)))
            .ReverseMap();

        CreateMap<InfoBase, InfoBaseDto>().ReverseMap();

        CreateMap<MaintenanceStep, MaintenanceStepDto>().ReverseMap();
        CreateMap<MaintenanceTask, MaintenanceTaskDto>().ReverseMap();
        CreateMap<MaintenanceStepLogItem, MaintenanceStepLogItemDto>().ReverseMap();
    }
}