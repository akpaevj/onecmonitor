using AutoMapper;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.ViewModels.Agents;
using OnecMonitor.Server.ViewModels.Clusters;
using OnecMonitor.Server.ViewModels.Configurations;
using OnecMonitor.Server.ViewModels.InfoBases;
using OnecMonitor.Server.ViewModels.InfoBases.Index;
using OnecMonitor.Server.ViewModels.TechLogSeances;
using OnecMonitor.Server.ViewModels.UpdateInfoBaseTasks;

namespace OnecMonitor.Server.AutoMapper;

public class CommonProfile : Profile
{
    public CommonProfile()
    {
        CreateMap<Agent, SelectableItem>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.InstanceName))
            .ReverseMap();
        CreateMap<InfoBase, SelectableItem>().ReverseMap();
        CreateMap<Cluster, SelectableItem>().ReverseMap();
        CreateMap<LogTemplate, SelectableItem>().ReverseMap();
        CreateMap<V8Configuration, SelectableItem>()
            .ForMember(c => c.Name, opt => opt.MapFrom(src => $"{src.Name} ({src.Version})"))
            .ReverseMap();

        CreateMap<Cluster, ClusterEditViewModel>()
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore())
            .ForMember(c => c.InfoBases, i => i.Ignore());

        CreateMap<TechLogSeance, TechLogSeancesListItemViewModel>().ReverseMap();
        
        CreateMap<TechLogSeance, TechLogSeanceEditViewModel>()
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore())
            .ForMember(c => c.Templates, i => i.Ignore())
            .ForMember(c => c.Agents, i => i.Ignore());

        CreateMap<InfoBase, InfoBaseListItemViewModel>()
            .ForMember(c => c.Cluster, i => i.MapFrom(a => a.Cluster.Name));
        
        CreateMap<InfoBase, InfoBaseEditViewModel>()
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore())
            .ForMember(c => c.Cluster, i => i.Ignore());

        CreateMap<V8Configuration, ConfigurationViewModel>()
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore());

        CreateMap<UpdateInfoBaseTask, UpdateInfoBaseTaskListItemViewModel>()
            .ForMember(dest => dest.IsStarted, i => i.MapFrom(src => src.StartDateTime != DateTime.MinValue))
            .ForMember(dest => dest.IsFaulted, i => i.MapFrom(src => src.Results.Any(result => result.IsFaulted)))
            .ForMember(dest => dest.IsFinished, i => i.MapFrom(src => src.Results.Count > 0 && src.Results.All(result => result.FinishDateTime != DateTime.MinValue)));

        CreateMap<UpdateInfoBaseTask, UpdateInfoBaseTaskEditViewModel>()
            .ForMember(c => c.NeedUpdateConfiguration, i => i.MapFrom(src => src.ConfigurationId != Guid.Empty))
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore())
            .ForMember(c => c.Configuration, i => i.Ignore())
            .ForMember(c => c.Extensions, i => i.Ignore())
            .ForMember(c => c.InfoBases, i => i.Ignore())
            .ForMember(c => c.Results, i => i.Ignore());
    }
}