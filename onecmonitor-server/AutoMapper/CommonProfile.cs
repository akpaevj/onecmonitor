using AutoMapper;
using OnecMonitor.Server.Dto.ErrorLoggingService;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.Models.MaintenanceTasks;
using OnecMonitor.Server.ViewModels.Agents;
using OnecMonitor.Server.ViewModels.Clusters;
using OnecMonitor.Server.ViewModels.Credentials;
using OnecMonitor.Server.ViewModels.Dbms;
using OnecMonitor.Server.ViewModels.ErrorLoggingService;
using OnecMonitor.Server.ViewModels.ErrorLoggingService.Index;
using OnecMonitor.Server.ViewModels.EventLogSettings;
using OnecMonitor.Server.ViewModels.InfoBases;
using OnecMonitor.Server.ViewModels.MaintenanceTasks;
using OnecMonitor.Server.ViewModels.TechLogSeances;
using OnecMonitor.Server.ViewModels.TechLogSettings;
using OnecMonitor.Server.ViewModels.V8Files;

namespace OnecMonitor.Server.AutoMapper;

public class CommonProfile : Profile
{
    public CommonProfile()
    {
        CreateMap<Agent, SelectableItemViewModel>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.InstanceName))
            .ReverseMap();
        CreateMap<Dbms, SelectableItemViewModel>().ReverseMap();
        CreateMap<InfoBase, SelectableItemViewModel>().ReverseMap();
        CreateMap<Cluster, SelectableItemViewModel>().ReverseMap();
        CreateMap<LogTemplate, SelectableItemViewModel>().ReverseMap();
        CreateMap<Credentials, SelectableItemViewModel>().ReverseMap();
        CreateMap<MaintenanceTask, SelectableItemViewModel>()
            .ForMember(c => c.Name, opt => opt.MapFrom(src => src.Description.ToString()))
            .ReverseMap();
        CreateMap<V8File, SelectableItemViewModel>()
            .ForMember(c => c.Name, opt => opt.MapFrom(src => src.ToString()))
            .ReverseMap();

        CreateMap<Agent, AgentViewModel>();
        
        CreateMap<Credentials, CredentialsListItemViewModel>()
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore());
        
        CreateMap<Credentials, CredentialsEditViewModel>()
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore())
            .ForMember(c => c.InfoBases, i => i.Ignore())
            .ForMember(c => c.Clusters, i => i.Ignore());
        
        CreateMap<Dbms, DbmsListItemViewModel>()
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore());
        
        CreateMap<Dbms, DbmsEditViewModel>()
            .ForMember(c => c.Types, i => i.Ignore())
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore());

        CreateMap<V8File, V8FileListItemViewModel>()
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore());
        
        CreateMap<V8File, V8FileEditViewModel>()
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore());

        CreateMap<Cluster, ClusterViewModel>();

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

        CreateMap<InfoBase, InfoBaseViewModel>();
        
        CreateMap<InfoBase, InfoBaseEditViewModel>()
            .ForMember(c => c.Credentials, i => i.Ignore())
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore())
            .ForMember(c => c.Cluster, i => i.Ignore());

        CreateMap<V8File, V8FileEditViewModel>()
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore());

        CreateMap<TechLogSettings, TechLogSettingsEditViewModel>()
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore());
        
        CreateMap<EventLogSettings, EventLogSettingsEditViewModel>()
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore());
        
        CreateMap<ErrorLoggingServiceSettings, ErrorLoggingServiceSettingsViewModel>()
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore());
        
        CreateMap<ReportRoot, ErrorLoggingServiceListItemViewModel>()
            .ReverseMap()
            .ForMember(c => c.Id, i => i.Ignore());
        
        CreateMap<MaintenanceTask, MaintenanceTaskEditViewModel>()
            .ReverseMap()
            .ForMember(c => c.Steps, i => i.Ignore())
            .ForMember(c => c.Id, i => i.Ignore())
            .ForMember(c => c.InfoBases, i => i.Ignore());

        CreateMap<MaintenanceTask, MaintenanceTaskListItemViewModel>();

        CreateMap<MaintenanceStep, MaintenanceStepViewModel>()
            .ForMember(c => c.Files, i => i.Ignore())
            .ForMember(c => c.Kinds, i => i.Ignore())
            .ReverseMap()
            .ForMember(c => c.File, i => i.Ignore());

        CreateMap<MaintenanceStep, MaintenanceStep>();
    }
}