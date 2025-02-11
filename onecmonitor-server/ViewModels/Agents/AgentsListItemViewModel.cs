using System.ComponentModel;

namespace OnecMonitor.Server.ViewModels.Agents
{
    public class AgentsListItemViewModel
    {
        [DisplayName("Идентификатор")]
        public Guid Id { get; set; }
        [DisplayName("Имя экземпляра")]
        public string InstanceName { get; set; } = string.Empty;
        [DisplayName("Подключен")]
        public bool IsConnected { get; set; }
    }
}