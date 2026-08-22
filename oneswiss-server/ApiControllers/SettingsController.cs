using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace OneSwiss.Server.ApiControllers;

[ApiController]
[Route("api/settings")]
public class SettingsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("summary")]
    [Authorize]
    public async Task<SettingsSummaryResponse> GetSummary(CancellationToken cancellationToken)
    {
        var techLogEnabled = await dbContext.TechLogSettings
            .AsNoTracking()
            .Select(c => c.Enabled)
            .SingleOrDefaultAsync(cancellationToken);

        var eventLogEnabled = await dbContext.EventLogSettings
            .AsNoTracking()
            .Select(c => c.Enabled)
            .SingleOrDefaultAsync(cancellationToken);

        var errorLoggingEnabled = await dbContext.ErrorLoggingServiceSettings
            .AsNoTracking()
            .Select(c => c.Enabled)
            .SingleOrDefaultAsync(cancellationToken);

        var crProxyEnabled = await dbContext.CrServerProxySettings
            .AsNoTracking()
            .Select(c => c.Enabled)
            .SingleOrDefaultAsync(cancellationToken);

        var hasTelegramToken = await dbContext.TelegramBotSettings
            .AsNoTracking()
            .Select(c => !string.IsNullOrEmpty(c.Token))
            .SingleOrDefaultAsync(cancellationToken);

        var accessGroupsCount = await dbContext.AccessGroups.AsNoTracking().CountAsync(cancellationToken);
        var usersGroupsCount = await dbContext.UsersGroups.AsNoTracking().CountAsync(cancellationToken);
        var usersCount = await dbContext.Users.AsNoTracking().CountAsync(cancellationToken);
        var recipientsCount = await dbContext.NotificationRecipients.AsNoTracking().CountAsync(cancellationToken);
        var customNotificationsCount = await dbContext.CustomNotifications.AsNoTracking().CountAsync(cancellationToken);

        return new SettingsSummaryResponse(
            techLogEnabled,
            eventLogEnabled,
            errorLoggingEnabled,
            crProxyEnabled,
            hasTelegramToken,
            accessGroupsCount,
            usersGroupsCount,
            usersCount,
            recipientsCount,
            customNotificationsCount);
    }

    public sealed record SettingsSummaryResponse(
        bool TechLogEnabled,
        bool EventLogEnabled,
        bool ErrorLoggingEnabled,
        bool CrProxyEnabled,
        bool HasTelegramToken,
        int AccessGroupsCount,
        int UsersGroupsCount,
        int UsersCount,
        int NotificationRecipientsCount,
        int CustomNotificationsCount);
}
