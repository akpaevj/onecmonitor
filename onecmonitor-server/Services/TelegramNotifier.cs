using Microsoft.EntityFrameworkCore;
using OnecMonitor.Server.Views.Settings;
using Telegram.Bot;

namespace OnecMonitor.Server.Services;

public class TelegramNotifier(AppDbContext dbContext, ILogger<TelegramNotifier> logger)
{
    private Queue<string> _messages = new();
    private string _token = string.Empty;

    public async Task UpdateSettings(CancellationToken cancellationToken)
    {
        var count = await dbContext.CommonSettings.CountAsync(cancellationToken);
        if (count == 0)
        {
            await dbContext.CommonSettings.AddAsync(new CommonSettings(), cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        
        var settings = await dbContext.CommonSettings.SingleAsync(cancellationToken);

        var options = new TelegramBotClientOptions(settings.TelegramBotToken);
        var client = new TelegramBotClient(options);
    }

    public void NotifyMaintenanceTaskCompleted(Guid taskId)
    {
        var task = dbContext.MaintenanceTasks
            .AsNoTracking()
            .FirstOrDefault(c => c.Id == taskId);
        
        if (task == null)
        {
            logger.LogWarning($"Задача с id {taskId} не найдена");
            return;
        }

        var message = $"Задача \"{task.Description}\" (Начало - ${task.StartDateTime}) завершена. Время выполнения - ${(task.FinishDateTime - task.StartDateTime).TotalMinutes} минут";
        _messages.Enqueue(message);
    }
}