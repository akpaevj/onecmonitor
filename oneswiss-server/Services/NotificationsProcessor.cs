using Microsoft.EntityFrameworkCore;
using OneSwiss.Server.Hubs;
using OneSwiss.Server.Models;
using Telegram.Bot;
using Telegram.Bot.Extensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace OneSwiss.Server.Services;

public class NotificationsProcessor(IDbContextFactory<AppDbContext> dbContextFactory, ILogger<NotificationsProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var context = await dbContextFactory.CreateDbContextAsync(stoppingToken);

                var settings = await context.TelegramBotSettings.SingleOrDefaultAsync(stoppingToken);
                var botClient = string.IsNullOrEmpty(settings?.Token) ? null : new TelegramBotClient(settings.Token);
                
                var notifications = await context.Notifications.ToListAsync(stoppingToken);
                
                foreach (var notification in notifications)
                {
                    if (botClient != null && notification.Channel == NotificationChannel.Telegram)
                    {
                        var chatId = new ChatId(notification.Recipient);

                        if (notification.Type == NotificationType.ErrorReportReceived)
                            await SendErrorReportReceived(context, botClient, chatId, notification, stoppingToken);
                        else 
                            await botClient.SendMessage(chatId, notification.Message, ParseMode.MarkdownV2, cancellationToken: stoppingToken);
                    }

                    if (notification.Channel == NotificationChannel.WebHook)
                    {
                        using var client = new HttpClient();
                        await client.PostAsJsonAsync(
                            notification.Recipient, 
                            new WebHookPayload(notification.Type, notification.Message), 
                            stoppingToken);
                    }
                        
                    context.Entry(notification).State = EntityState.Deleted;
                }
                
                await context.SaveChangesAsync(stoppingToken);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Ошибка отправки уведомления в Telegram");
            }
            
            await Task.Delay(15 * 1000, stoppingToken);
        }
    }

    private static async Task SendErrorReportReceived(
        AppDbContext context, 
        TelegramBotClient botClient, 
        ChatId chatId,
        Notification notification, 
        CancellationToken cancellationToken)
    {
        await botClient.SendMessage(chatId, notification.Message, ParseMode.MarkdownV2, cancellationToken: cancellationToken);
        
        var item = context.ErrorReports.AsNoTracking().FirstOrDefault(x => x.Id == Guid.Parse(notification.Additionalinfo));
        if (item is { Screenshot.Length: > 0 })
        {
            using var memoryStream = new MemoryStream(item.Screenshot);
            var file = InputFile.FromStream(memoryStream);

            await botClient.SendPhoto(chatId, file, cancellationToken: cancellationToken);
        }
    }

    private record WebHookPayload(NotificationType Type, string Message);
}