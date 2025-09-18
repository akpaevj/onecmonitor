using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Common.Models;
using OneSwiss.Server.Dto.ErrorLoggingService;
using OneSwiss.Server.Helpers;
using OneSwiss.Server.Models;
using Telegram.Bot;
using Telegram.Bot.Extensions;

namespace OneSwiss.Server.Services;

public class NotificationsService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ILogger<NotificationsService> logger)
{
    public async Task QueueErrorReportReceived(Guid id, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var item = await dbContext.ErrorReports.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (item == null)
            return;

        var report = JsonSerializer.Deserialize<ReportRoot>(item.Report, ErrorReportsHelper.ReportSerializerOptions);

        var messageBuilder = new StringBuilder("‼️ Получен новый отчет об ошибке\n");

        if (!string.IsNullOrEmpty(report?.SessionInfo.UserName))
            messageBuilder.AppendLine($"👤 Пользователь: {Markdown.Escape(report.SessionInfo.UserName)}");

        messageBuilder.AppendLine($"""
                                   💬 Сообщение:
                                   ```
                                   {Markdown.Escape(report!.ErrorInfo.ApplicationErrorInfo.Errors[0].Text)}
                                   ```
                                   👣 Стек:
                                   ```js
                                   {Markdown.Escape(report.GetStack())}
                                   ```
                                   """);

        if (report.AdditionalInfo != null)
            messageBuilder.AppendLine($"""
                                       ℹ️ Дополнительная информация:
                                       ```
                                       {Markdown.Escape(report.AdditionalInfo)}
                                       ```
                                       """);

        await QueueNotification(dbContext, NotificationType.ErrorReportReceived, messageBuilder.ToString(),
            cancellationToken, id.ToString());
    }

    public async Task QueueMaintenanceTaskCompleted(Guid id, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var task = await dbContext.MaintenanceTasks.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (task == null)
            return;

        var stateIcon = task.IsFaulted ? "‼️" : "✅";
        var stateText = task.IsFaulted ? "с ошибками" : "успешно";

        var message = $"""
                       {stateIcon} Задача завершена {stateText}
                       📝 Описание: {Markdown.Escape(task.Description)}
                       🕜 Начало: {Markdown.Escape(task.StartDateTime.ToString(CultureInfo.CurrentCulture))}
                       🏁 Окончание: {Markdown.Escape(task.FinishDateTime.ToString(CultureInfo.CurrentCulture))}
                       ⏱️ Время выполнения: {Math.Round((task.FinishDateTime - task.StartDateTime).TotalMinutes)} минут
                       """;

        await QueueNotification(dbContext, NotificationType.MaintenanceTaskCompleted, message, cancellationToken);
    }

    public async Task QueueGitSyncStopped(Guid id, Guid itemId, string reason, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var item = await dbContext.GitSyncTasks
            .Include(gitSyncTask => gitSyncTask.GitRepository)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (item == null)
            return;

        string message;

        if (itemId == Guid.Empty)
        {
            message = $"""
                       ‼️ Синхронизация репозитория "{Markdown.Escape(item.GitRepository.Name)}" остановлена
                       📝 Причина: 
                       {Markdown.Escape(reason)}
                       """;
        }
        else
        {
            var configRepository =
                await dbContext.ConfigRepositories.FirstOrDefaultAsync(c => c.Id == itemId, cancellationToken);

            if (configRepository == null)
                return;

            message = $"""
                       ‼️ Синхронизация хранилища "{Markdown.Escape(configRepository.Name)}" остановлена
                       ℹ️ Репозиторий Git:
                       {Markdown.Escape(item.GitRepository.Name)}
                       📝 Причина: 
                       {Markdown.Escape(reason)}
                       """;
        }

        await QueueNotification(dbContext, NotificationType.GitSyncStopped, message, cancellationToken);
    }

    public async Task QueueCustomNotification(string key, string message, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        try
        {
            var recipients = await dbContext.NotificationRecipients
                .Where(c => c.CustomNotifications.FirstOrDefault(i => i.Key == key) != null)
                .ToListAsync(cancellationToken);

            foreach (var recipient in recipients)
                await dbContext.Notifications.AddAsync(new Notification
                {
                    CreatedAt = DateTime.Now,
                    Type = NotificationType.Custom,
                    Recipient = recipient.SendTo,
                    Channel = recipient.Channel,
                    Message = message
                }, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка при добавлении уведомлений");
        }
    }

    private async Task QueueNotification(AppDbContext dbContext, NotificationType notificationType, string message,
        CancellationToken cancellationToken, string addInfo = "")
    {
        try
        {
            var recipients = await dbContext.NotificationRecipients
                .Where(c => c.NotificationTypes.Contains(notificationType))
                .ToListAsync(cancellationToken);

            foreach (var recipient in recipients)
                await dbContext.Notifications.AddAsync(new Notification
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.Now,
                    Type = notificationType,
                    Recipient = recipient.SendTo,
                    Channel = recipient.Channel,
                    Message = message,
                    Additionalinfo = addInfo
                }, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка при добавления уведомлений");
        }
    }

    public static async Task<bool> CheckTelegramBotTokenIsValid(string token, CancellationToken cancellationToken)
    {
        var options = new TelegramBotClientOptions(token);
        var client = new TelegramBotClient(options);

        return await client.TestApi(cancellationToken);
    }
}