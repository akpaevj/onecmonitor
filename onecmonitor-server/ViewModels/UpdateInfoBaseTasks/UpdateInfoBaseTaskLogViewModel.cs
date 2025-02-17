using OnecMonitor.Server.Models;
using OnecMonitor.Server.ViewModels.InfoBases;

namespace OnecMonitor.Server.ViewModels.UpdateInfoBaseTasks;

public class UpdateInfoBaseTaskLogViewModel
{
    public Guid TaskId { get; set; }
    public List<(InfoBase InfoBase, bool IsStarted, bool IsFinished)> InfoBases { get; set; } = [];
    public List<UpdateInfoBaseTaskLogItem> Log { get; set; } = [];
}