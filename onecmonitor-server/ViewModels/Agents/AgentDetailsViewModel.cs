using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using OneSTools.Common.Platform;
using OneSTools.Common.Platform.Services;

namespace OnecMonitor.Server.ViewModels.Agents;

[ValidateNever]
[DisplayName("Агент монитора")]
public class AgentDetailsViewModel
{
    public Guid Id { get; set; }
    [DisplayName("Имя экземпляра")]
    public string InstanceName { get; set; } = string.Empty;
    [DisplayName("Подключен")]
    public bool IsConnected { get; set; }
    [DisplayName("Установленные платформы")]
    public List<V8Platform> InstalledPlatforms { get; set; } = [];
    [DisplayName("Службы агентов сервера 1С")]
    public List<RagentService> RagentServices { get; set; } = [];
    [DisplayName("Службы сервера администрирования 1С")]
    public List<RasService> RasServices { get; set; } = [];
}