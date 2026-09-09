using System.Text.RegularExpressions;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Common.DTO;
using OneSwiss.Common.Storage;
using OneSwiss.Server.Services;

namespace OneSwiss.Server.ApiControllers;

[ApiController]
[Route("api/eventlog/settings")]
public class EventLogSettingsController(
    AppDbContext dbContext,
    IMapper mapper,
    AgentsConnectionsManager agentsConnectionsManager) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<EventLogSettingsResponse> Get(CancellationToken cancellationToken)
    {
        var settings = await dbContext.EventLogSettings
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .SingleOrDefaultAsync(cancellationToken);

        var exportItems = await dbContext.EventLogExportItems
            .AsNoTracking()
            .OrderBy(i => i.InfoBaseId)
            .Select(i => new EventLogExportItemRequest(i.InfoBaseId, i.IsActive, i.Ttl, i.ReduceSourceLog,
                i.ReduceKeepDays, i.LastReducedUpTo))
            .ToListAsync(cancellationToken);

        var dbmsItems = await dbContext.Dbms
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new LookupItem(d.Id, d.Name))
            .ToListAsync(cancellationToken);

        var credentialsItems = await dbContext.Credentials
            .AsNoTracking()
            .Where(c => !c.IsToken)
            .OrderBy(c => c.Name)
            .Select(c => new LookupItem(c.Id, c.Name))
            .ToListAsync(cancellationToken);

        var infoBaseItems = await dbContext.InfoBases
            .AsNoTracking()
            .OrderBy(i => i.Name)
            .Select(i => new LookupItem(i.Id, i.Name))
            .ToListAsync(cancellationToken);

        return new EventLogSettingsResponse(
            new EventLogSettingsItem(
                settings?.Enabled ?? false,
                settings?.DbmsId,
                settings?.DatabaseName ?? string.Empty,
                settings?.Table ?? string.Empty,
                settings?.CredentialsId,
                settings?.InfoBaseNameRegex ?? string.Empty,
                settings?.DefaultTtl ?? 365,
                settings?.ReductionEnabled ?? false,
                settings?.ReductionHourUtc ?? 2,
                settings?.ReductionSafetyMarginHours ?? 24),
            exportItems,
            dbmsItems,
            credentialsItems,
            infoBaseItems);
    }

    [HttpPut]
    [Authorize]
    public async Task<ActionResult<EventLogSettingsItem>> Save([FromBody] SaveEventLogSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateRequest(request, cancellationToken);
        if (validationError != null)
            return validationError;

        var settings = await dbContext.EventLogSettings
            .OrderBy(s => s.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (settings == null)
        {
            settings = new Models.EventLogSettings();
            dbContext.EventLogSettings.Add(settings);
        }

        settings.Enabled = request.Enabled;
        settings.DbmsId = request.DbmsId;
        settings.DatabaseName = request.DatabaseName.Trim();
        settings.Table = request.Table.Trim();
        settings.CredentialsId = request.CredentialsId;
        settings.InfoBaseNameRegex = request.InfoBaseNameRegex.Trim();
        settings.DefaultTtl = request.DefaultTtl;
        settings.ReductionEnabled = request.ReductionEnabled;
        settings.ReductionHourUtc = request.ReductionHourUtc;
        settings.ReductionSafetyMarginHours = request.ReductionSafetyMarginHours;

        var existingItems = await dbContext.EventLogExportItems.ToListAsync(cancellationToken);
        var requestItems = request.Items
            .GroupBy(i => i.InfoBaseId)
            .Select(g => g.First())
            .ToDictionary(i => i.InfoBaseId, i => i);

        foreach (var existingItem in existingItems)
        {
            if (requestItems.TryGetValue(existingItem.InfoBaseId, out var requested))
            {
                existingItem.IsActive = requested.IsActive;
                existingItem.Ttl = requested.Ttl;
                existingItem.ReduceSourceLog = requested.ReduceSourceLog;
                existingItem.ReduceKeepDays = requested.ReduceKeepDays;
                requestItems.Remove(existingItem.InfoBaseId);
            }
            else
            {
                dbContext.EventLogExportItems.Remove(existingItem);
            }
        }

        foreach (var newItem in requestItems.Values)
        {
            dbContext.EventLogExportItems.Add(new Models.EventLogExportItem
            {
                InfoBaseId = newItem.InfoBaseId,
                IsActive = newItem.IsActive,
                Ttl = newItem.Ttl,
                ReduceSourceLog = newItem.ReduceSourceLog,
                ReduceKeepDays = newItem.ReduceKeepDays
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (settings.Enabled)
            await InitEventLogTable(settings, cancellationToken);

        await agentsConnectionsManager.RaiseUpdateSettings(cancellationToken);

        return Ok(new EventLogSettingsItem(
            settings.Enabled,
            settings.DbmsId,
            settings.DatabaseName,
            settings.Table,
            settings.CredentialsId,
            settings.InfoBaseNameRegex,
            settings.DefaultTtl,
            settings.ReductionEnabled,
            settings.ReductionHourUtc,
            settings.ReductionSafetyMarginHours));
    }

    private async Task InitEventLogTable(Models.EventLogSettings settings, CancellationToken cancellationToken)
    {
        var dbms = await dbContext.Dbms
            .AsNoTracking()
            .SingleAsync(d => d.Id == settings.DbmsId, cancellationToken);

        var credentials = await dbContext.Credentials
            .AsNoTracking()
            .SingleAsync(c => c.Id == settings.CredentialsId, cancellationToken);

        var dbmsDto = mapper.Map<DbmsDto>(dbms);
        var credentialsDto = mapper.Map<CredentialsDto>(credentials);

        using var context = new ClickHouseContext(dbmsDto, credentialsDto, settings.DatabaseName, settings.Table);
        await context.InitEventLogTable(cancellationToken);
    }

    private async Task<ActionResult?> ValidateRequest(SaveEventLogSettingsRequest request, CancellationToken cancellationToken)
    {
        if (request.DefaultTtl <= 0)
            return BadRequest("TTL по умолчанию должен быть больше 0");

        if (request.Items.Any(i => i.Ttl <= 0))
            return BadRequest("TTL экспортируемых журналов должен быть больше 0");

        if (request.Items.Any(i => i.ReduceSourceLog && i.ReduceKeepDays <= 0))
            return BadRequest("Количество дней хранения журнала на источнике должно быть больше 0");

        if (request.ReductionEnabled && (request.ReductionHourUtc < 0 || request.ReductionHourUtc > 23))
            return BadRequest("Час запуска свёртки журнала должен быть в диапазоне 0-23");

        if (request.ReductionEnabled && request.ReductionSafetyMarginHours < 0)
            return BadRequest("Запас перед подтверждённой точкой экспорта не может быть отрицательным");

        var duplicatedInfoBases = request.Items
            .GroupBy(i => i.InfoBaseId)
            .Any(g => g.Count() > 1);

        if (duplicatedInfoBases)
            return BadRequest("Одна и та же информационная база указана в экспорте несколько раз");

        if (request.Items.Count > 0)
        {
            var infoBaseIds = request.Items.Select(i => i.InfoBaseId).Distinct().ToArray();
            var existingInfoBases = await dbContext.InfoBases
                .AsNoTracking()
                .CountAsync(i => infoBaseIds.Contains(i.Id), cancellationToken);

            if (existingInfoBases != infoBaseIds.Length)
                return BadRequest("Указаны несуществующие информационные базы");
        }

        if (!request.Enabled)
            return null;

        if (request.DbmsId == null || request.DbmsId == Guid.Empty)
            return BadRequest("Не указана СУБД");

        if (request.CredentialsId == null || request.CredentialsId == Guid.Empty)
            return BadRequest("Не указаны учетные данные");

        if (string.IsNullOrWhiteSpace(request.DatabaseName))
            return BadRequest("Не указано имя базы данных");

        if (string.IsNullOrWhiteSpace(request.Table))
            return BadRequest("Не указана таблица базы данных");

        if (string.IsNullOrWhiteSpace(request.InfoBaseNameRegex))
            return BadRequest("Не указано регулярное выражение для имени информационной базы");

        if (!IsValidRegex(request.InfoBaseNameRegex))
            return BadRequest("Указано невалидное регулярное выражение");

        var dbmsExists = await dbContext.Dbms
            .AsNoTracking()
            .AnyAsync(d => d.Id == request.DbmsId, cancellationToken);

        if (!dbmsExists)
            return BadRequest("Указана несуществующая СУБД");

        var credentialsExists = await dbContext.Credentials
            .AsNoTracking()
            .AnyAsync(c => c.Id == request.CredentialsId && !c.IsToken, cancellationToken);

        if (!credentialsExists)
            return BadRequest("Указаны некорректные учетные данные");

        return null;
    }

    private static bool IsValidRegex(string pattern)
    {
        try
        {
            _ = Regex.Match(string.Empty, pattern);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public sealed record SaveEventLogSettingsRequest(
        bool Enabled,
        Guid? DbmsId,
        string DatabaseName,
        string Table,
        Guid? CredentialsId,
        string InfoBaseNameRegex,
        int DefaultTtl,
        bool ReductionEnabled,
        int ReductionHourUtc,
        int ReductionSafetyMarginHours,
        IReadOnlyList<EventLogExportItemRequest> Items);

    public sealed record EventLogSettingsResponse(
        EventLogSettingsItem Settings,
        IReadOnlyList<EventLogExportItemRequest> Items,
        IReadOnlyList<LookupItem> Dbms,
        IReadOnlyList<LookupItem> Credentials,
        IReadOnlyList<LookupItem> InfoBases);

    public sealed record EventLogSettingsItem(
        bool Enabled,
        Guid? DbmsId,
        string DatabaseName,
        string Table,
        Guid? CredentialsId,
        string InfoBaseNameRegex,
        int DefaultTtl,
        bool ReductionEnabled,
        int ReductionHourUtc,
        int ReductionSafetyMarginHours);

    public sealed record EventLogExportItemRequest(
        Guid InfoBaseId,
        bool IsActive,
        int Ttl,
        bool ReduceSourceLog,
        int ReduceKeepDays,
        DateTime? LastReducedUpTo);

    public sealed record LookupItem(
        Guid Id,
        string Name);
}
