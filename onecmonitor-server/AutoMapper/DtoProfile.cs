using AutoMapper;
using OnecMonitor.Common.DTO;
using OnecMonitor.Server.Models;

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
            .ReverseMap();

        CreateMap<InfoBase, InfoBaseDto>().ReverseMap();

        CreateMap<UpdateInfoBaseTask, UpdateInfoBaseTaskDto>().ReverseMap();

        CreateMap<UpdateInfoBaseTaskLogItemDto, UpdateInfoBaseTaskLogItem>();
    }
}