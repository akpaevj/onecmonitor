using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace OnecMonitor.Server.Hubs;

public class MaintenanceTaskLogsHub(AppDbContext appDbContext) : Hub
{
    public async Task GetLogs(string taskId)
    {
        var task = await appDbContext.MaintenanceTasks
            .AsNoTracking()
            .Include(c => c.InfoBases)
            .Include(c => c.Steps)
                .ThenInclude(c => c.Logs)
            .FirstOrDefaultAsync(c => c.Id == Guid.Parse(taskId));
        
        var logs = task!.Steps.SelectMany(s => s.Logs).OrderBy(c => c.TimeStamp).ToList();
        
        var items = task.InfoBases.Select(c => new
        {
            InfoBase = new
            {
                Id = c.Id,
                Name = c.Name
            },
            Log = logs.Where(i => i.InfoBaseId == c.Id).Select(l => new
            {
                Id = l.Id,
                InfoBaseId = l.InfoBaseId,
                TimeStamp = l.TimeStamp.ToString("yyyy-MM-dd HH:mm:ss"),
                IsError = l.IsError,
                IsFinish = l.IsFinish,
                Message = l.Message
            })
        }).ToArray();
        
        await Clients.Caller.SendAsync("HandleLogs", items);
    }
}