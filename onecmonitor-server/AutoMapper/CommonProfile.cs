using AutoMapper;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.Models.MaintenanceTasks;
using OnecMonitor.Server.ViewModels.Clusters;
using OnecMonitor.Server.ViewModels.V8Files;
using OnecMonitor.Server.ViewModels.Credentials;
using OnecMonitor.Server.ViewModels.InfoBases;
using OnecMonitor.Server.ViewModels.MaintenanceTasks;
using OnecMonitor.Server.ViewModels.TechLogSeances;
using OnecMonitor.Server.ViewModels.TechLogSettings;
using OnecMonitor.Server.ViewModels.UpdateInfoBaseTasks;

namespace OnecMonitor.Server.AutoMapper;

public class CommonProfile : Profile
{
    public CommonProfile()
    {
        CreateMap<Agent, SelectableItemViewModel>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.InstanceName))
            .ReverseMap();
        CreateMap<InfoBase, SelectableItemViewModel>().ReverseMap();
        CreateMap<Cluster, SelectableItemViewModel>().ReverseMap();
        CreateMap<LogTemplate, SelectableItemViewModel>().ReverseMap();
        CreateMap<Credentials, SelectableItemViewModel>().ReverseMap();
        CreateMap<V8File, SelectableItemViewModel>()
            .ForMember(c => c.Name, opt => opt.MapFrom(src => src.ToString()))
            .ReverseMap();
        
        CreateMap<Credentials, CredentialsListItemViewModel>()
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore());
        
        CreateMap<Credentials, CredentialsEditViewModel>()
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore())
            .ForMember(c => c.InfoBases, i => i.Ignore())
            .ForMember(c => c.Clusters, i => i.Ignore());

        CreateMap<V8File, V8FileListItemViewModel>()
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore());
        
        CreateMap<V8File, V8FileEditViewModel>()
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore());

        CreateMap<Cluster, ClusterEditViewModel>()
            .ForMember(c => c.Credentials, i => i.Ignore())
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
            .ForMember(c => c.Credentials, i => i.Ignore())
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore())
            .ForMember(c => c.Cluster, i => i.Ignore());

        CreateMap<V8File, V8FileEditViewModel>()
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore());

        CreateMap<UpdateInfoBaseTask, UpdateInfoBaseTaskListItemViewModel>()
            .ForMember(dest => dest.IsStarted, i => i.MapFrom(src => src.StartDateTime != DateTime.MinValue))
            .ForMember(dest => dest.IsFaulted, i => i.MapFrom(src => src.Log.Any(result => result.IsError)))
            .ForMember(dest => dest.IsFinished, i => i.MapFrom(src => src.Log.Any(result => result.IsFinish)));

        CreateMap<UpdateInfoBaseTask, UpdateInfoBaseTaskEditViewModel>()
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore())
            .ForMember(c => c.Files, i => i.Ignore())
            .ForMember(c => c.InfoBases, i => i.Ignore())
            .ForMember(c => c.Log, i => i.Ignore());

        CreateMap<TechLogSettings, TechLogSettingsEditViewModel>()
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore());
        
        CreateMap<MaintenanceTask, MaintenanceTaskEditViewModel>()
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore())
            .ForMember(c => c.InfoBases, i => i.Ignore());

        CreateMap<MaintenanceTask, MaintenanceTaskListItemViewModel>();
        
        CreateMap<MaintenanceStep, MaintenanceStepViewModel>()
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore())
            .ForMember(c => c.File, i => i.Ignore());
        
        CreateMap<MaintenanceStepNode, MaintenanceStepNodeViewModel>()
            .ForMember(c => c.StepId, i => i.MapFrom(src => src.Step.Id))
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore())
            .ForMember(c => c.LeftNode, i => i.Ignore())
            .ForMember(c => c.RightNode, i => i.Ignore())
            .ForMember(c => c.Step, i => i.Ignore());
    }
}