using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace OneSwiss.Server.Hubs;

[AllowAnonymous]
public class MaintenanceTaskLogHub : Hub
{
    public async Task Subscribe(Guid taskId)
    {
        if (Context.Items.TryAdd("taskId", taskId))
            await Groups.AddToGroupAsync(Context.ConnectionId, taskId.ToString());
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.Items.TryGetValue("taskId", out var taskId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ((Guid)taskId!).ToString());
    }
}