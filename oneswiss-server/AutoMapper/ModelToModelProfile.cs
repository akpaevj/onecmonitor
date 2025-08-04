using AutoMapper;
using OneSwiss.Server.Models.MaintenanceTasks;

namespace OneSwiss.Server.AutoMapper;

public class ModelToModelProfile : Profile
{
    public ModelToModelProfile()
    {
        CreateMap<CopyInfoBaseStep, CopyInfoBaseStep>().ReverseMap();
        CreateMap<LoadConfigurationStep, LoadConfigurationStep>().ReverseMap();
        CreateMap<LoadExtensionStep, LoadExtensionStep>().ReverseMap();
        CreateMap<UpdateConfigurationStep, UpdateConfigurationStep>().ReverseMap();
        CreateMap<ExecuteOneScriptStep, ExecuteOneScriptStep>().ReverseMap();
        CreateMap<StartExternalDataProcessorStep, StartExternalDataProcessorStep>().ReverseMap();
        CreateMap<LockConnectionsStep, LockConnectionsStep>().ReverseMap();
        CreateMap<DeleteExtensionStep, DeleteExtensionStep>().ReverseMap();
        CreateMap<MaintenanceStep, MaintenanceStep>().ReverseMap();
    }
}