using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Common.Extensions;
using OneSwiss.Common.Models;

namespace OneSwiss.Server.ApiControllers;

[ApiController]
[Route("api/notifications/settings")]
public class NotificationsSettingsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<NotificationsSettingsResponse> Get(CancellationToken cancellationToken)
    {
        var telegram = await dbContext.TelegramBotSettings
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .SingleOrDefaultAsync(cancellationToken);

        var customNotifications = await dbContext.CustomNotifications
            .AsNoTracking()
            .OrderBy(c => c.Key)
            .Select(c => new CustomNotificationItem(c.Key, c.Description))
            .ToListAsync(cancellationToken);

        var recipients = await dbContext.NotificationRecipients
            .AsNoTracking()
            .Include(r => r.CustomNotifications)
            .ToListAsync(cancellationToken);

        var recipientItems = recipients
            .Select(r => new NotificationRecipientItem(
                r.Id,
                r.Channel.ToString(),
                r.SendTo,
                r.NotificationTypes.Select(t => t.ToString()).ToList(),
                r.CustomNotifications.Select(c => c.Key).ToList()))
            .ToList();

        var channels = Enum.GetValues<Models.NotificationChannel>()
            .Select(c => new EnumOptionItem(c.ToString(), c.GetDisplay()))
            .ToList();

        var notificationTypes = Enum.GetValues<NotificationType>()
            .Select(c => new EnumOptionItem(c.ToString(), c.GetDisplay()))
            .ToList();

        return new NotificationsSettingsResponse(
            telegram?.Token ?? string.Empty,
            customNotifications,
            recipientItems,
            channels,
            notificationTypes);
    }

    [HttpPut]
    [Authorize]
    public async Task<IActionResult> Save([FromBody] SaveNotificationsSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(request);
        if (validationError != null)
            return validationError;

        if (!string.IsNullOrEmpty(request.TelegramToken))
        {
            var tokenIsValid = await Services.NotificationsService
                .CheckTelegramBotTokenIsValid(request.TelegramToken, cancellationToken);
            if (!tokenIsValid)
                return BadRequest("Невалидный токен или API недоступен");
        }

        var customByKey = new Dictionary<string, Models.CustomNotification>(StringComparer.InvariantCultureIgnoreCase);

        var telegram = await dbContext.TelegramBotSettings
            .OrderBy(t => t.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (telegram == null)
        {
            telegram = new Models.TelegramBotSettings();
            dbContext.TelegramBotSettings.Add(telegram);
        }

        telegram.Token = request.TelegramToken ?? string.Empty;

        var existingRecipients = await dbContext.NotificationRecipients
            .Include(r => r.CustomNotifications)
            .ToListAsync(cancellationToken);
        dbContext.NotificationRecipients.RemoveRange(existingRecipients);

        var existingCustomNotifications = await dbContext.CustomNotifications.ToListAsync(cancellationToken);
        dbContext.CustomNotifications.RemoveRange(existingCustomNotifications);

        foreach (var customItem in request.CustomNotifications)
        {
            var model = new Models.CustomNotification
            {
                Key = customItem.Key.Trim(),
                Description = customItem.Description ?? string.Empty
            };

            customByKey[model.Key] = model;
            dbContext.CustomNotifications.Add(model);
        }

        foreach (var recipient in request.Recipients)
        {
            if (!Enum.TryParse<Models.NotificationChannel>(recipient.Channel, true, out var channel))
                continue;

            var notificationTypes = recipient.NotificationTypes
                .Select(type => Enum.TryParse<NotificationType>(type, true, out var value)
                    ? value
                    : (NotificationType?)null)
                .Where(t => t != null)
                .Select(t => t!.Value)
                .Distinct()
                .ToList();

            var customNotifications = recipient.CustomNotificationKeys
                .Where(key => !string.IsNullOrWhiteSpace(key) && customByKey.ContainsKey(key.Trim()))
                .Select(key => customByKey[key.Trim()])
                .Distinct()
                .ToList();

            dbContext.NotificationRecipients.Add(new Models.NotificationRecipient
            {
                Channel = channel,
                SendTo = recipient.SendTo.Trim(),
                NotificationTypes = notificationTypes,
                CustomNotifications = customNotifications
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok();
    }

    private static ActionResult? ValidateRequest(SaveNotificationsSettingsRequest request)
    {
        var customKeys = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

        for (var index = 0; index < request.CustomNotifications.Count; index++)
        {
            var item = request.CustomNotifications[index];

            if (string.IsNullOrWhiteSpace(item.Key))
                return new BadRequestObjectResult($"В строке {index + 1} не указан ключ уведомления");

            var key = item.Key.Trim();
            if (key.Length > 30)
                return new BadRequestObjectResult($"Ключ не может содержать больше 30 символов (строка {index + 1})");

            if (!customKeys.Add(key))
                return new BadRequestObjectResult($"Повторяющийся ключ уведомления '{key}'");
        }

        for (var index = 0; index < request.Recipients.Count; index++)
        {
            var recipient = request.Recipients[index];

            if (!Enum.TryParse<Models.NotificationChannel>(recipient.Channel, true, out var channel))
                return new BadRequestObjectResult($"В строке {index + 1} указан невалидный канал");

            if (channel == Models.NotificationChannel.Telegram && string.IsNullOrWhiteSpace(recipient.SendTo))
                return new BadRequestObjectResult($"В строке {index + 1} не указан идентификатор пользователя");

            if (channel == Models.NotificationChannel.WebHook &&
                !Uri.TryCreate(recipient.SendTo, UriKind.Absolute, out _))
                return new BadRequestObjectResult($"В строке {index + 1} указан невалидный URI");

            foreach (var type in recipient.NotificationTypes)
            {
                if (!Enum.TryParse<NotificationType>(type, true, out _))
                    return new BadRequestObjectResult($"В строке {index + 1} указан невалидный тип уведомления");
            }

            foreach (var key in recipient.CustomNotificationKeys)
            {
                if (string.IsNullOrWhiteSpace(key) || !customKeys.Contains(key.Trim()))
                    return new BadRequestObjectResult($"В строке {index + 1} указано несуществующее пользовательское уведомление");
            }
        }

        return null;
    }

    public sealed record NotificationsSettingsResponse(
        string TelegramToken,
        IReadOnlyList<CustomNotificationItem> CustomNotifications,
        IReadOnlyList<NotificationRecipientItem> Recipients,
        IReadOnlyList<EnumOptionItem> Channels,
        IReadOnlyList<EnumOptionItem> NotificationTypes);

    public sealed record SaveNotificationsSettingsRequest(
        string TelegramToken,
        IReadOnlyList<CustomNotificationItem> CustomNotifications,
        IReadOnlyList<NotificationRecipientItem> Recipients);

    public sealed record NotificationRecipientItem(
        Guid Id,
        string Channel,
        string SendTo,
        IReadOnlyList<string> NotificationTypes,
        IReadOnlyList<string> CustomNotificationKeys);

    public sealed record CustomNotificationItem(
        string Key,
        string Description);

    public sealed record EnumOptionItem(
        string Value,
        string Display);
}
