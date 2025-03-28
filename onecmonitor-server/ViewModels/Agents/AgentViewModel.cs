using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace OnecMonitor.Server.ViewModels.Agents;

[ValidateNever]
[DisplayName("Агент монитора")]
public class AgentViewModel
{
    public Guid Id { get; set; }
    [DisplayName("Имя экземпляра")]
    public string InstanceName { get; set; } = string.Empty;
}