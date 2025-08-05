using System.Text.Json;
using AutoMapper;
using OneSwiss.Server.Components.Pages.ErrorLoggingService;
using OneSwiss.Server.Dto.ErrorLoggingService;
using OneSwiss.Server.Helpers;
using OneSwiss.Server.Models;

namespace OneSwiss.Server.AutoMapper;

public class ModelViewModelProfile : Profile
{
    public ModelViewModelProfile()
    {
        CreateMap<ErrorReport, ErrorReportViewModel>()
            .ConvertUsing((m, vm) =>
            {
                vm ??= new ErrorReportViewModel();
                
                var reportRoot = JsonSerializer.Deserialize<ReportRoot>(m.Report, ErrorReportsHelper.ReportSerializerOptions);

                vm.Id = m.Id;
                vm.Configuration = reportRoot!.ConfigInfo.Name;
                vm.ConfigurationVersion = reportRoot.ConfigInfo.Version;
                vm.Date = reportRoot.Time;
                vm.PlatformVersion = reportRoot.ServerInfo.AppVersion;
                vm.UserName = reportRoot.SessionInfo.UserName;
                vm.AdditionalInfo = reportRoot.AdditionalInfo ?? string.Empty;
                
                return vm;
            });

        CreateMap<ErrorReportViewModel, ErrorReport>();
    }
}