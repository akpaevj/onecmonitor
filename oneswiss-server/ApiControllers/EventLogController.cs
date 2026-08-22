using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Common.DTO;
using OneSwiss.Common.Helpers;

namespace OneSwiss.Server.ApiControllers;

[ApiController]
[Route("api/eventlog")]
public class EventLogController(
    AppDbContext dbContext,
    IMapper mapper) : ControllerBase
{
    [HttpGet("lookups")]
    [Authorize(Roles = Roles.ReadEventLog)]
    public async Task<ActionResult<EventLogLookupsResponse>> GetLookups(CancellationToken cancellationToken)
    {
        var infoBases = await dbContext.EventLogExportItems
            .AsNoTracking()
            .Include(c => c.InfoBase)
            .OrderBy(c => c.InfoBase.Name)
            .Select(c => new EventLogInfoBaseItem(
                c.InfoBaseId,
                c.InfoBase.InfoBaseInternalId,
                c.InfoBase.Name))
            .ToListAsync(cancellationToken);

        try
        {
            using var repository = await CreateRepository(cancellationToken);
            var eventTypes = await repository.GetEventsTypes(string.Empty, cancellationToken);

            return Ok(new EventLogLookupsResponse(infoBases, eventTypes));
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("items")]
    [Authorize(Roles = Roles.ReadEventLog)]
    public async Task<ActionResult<EventLogItemsResponse>> GetItems(
        [FromQuery] int pageSize = 30,
        [FromQuery] int page = 0,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid[]? infoBaseIds = null,
        [FromQuery] string[]? eventTypes = null,
        [FromQuery] string? filter = null,
        CancellationToken cancellationToken = default)
    {
        if (pageSize <= 0)
            return BadRequest("Размер страницы должен быть больше 0");

        if (page < 0)
            return BadRequest("Номер страницы не может быть отрицательным");

        var infoBases = await dbContext.EventLogExportItems
            .AsNoTracking()
            .Include(c => c.InfoBase)
            .OrderBy(c => c.InfoBase.Name)
            .Select(c => new EventLogInfoBaseItem(
                c.InfoBaseId,
                c.InfoBase.InfoBaseInternalId,
                c.InfoBase.Name))
            .ToListAsync(cancellationToken);

        var selectedInfoBaseInternalIds = infoBases
            .Where(c => infoBaseIds != null && infoBaseIds.Contains(c.Id))
            .Select(c => c.InternalId)
            .ToArray();

        var preparedFilter = PrepareFilter(
            selectedInfoBaseInternalIds,
            startDate,
            endDate,
            eventTypes,
            filter);

        try
        {
            using var repository = await CreateRepository(cancellationToken);

            var items = await repository.GetEventLogItems(pageSize, page * pageSize, preparedFilter, cancellationToken);
            items.ForEach(c => c.Date = c.Date.ToLocalTime());

            var totalItems = await repository.GetRowsCount(preparedFilter, cancellationToken);

            return Ok(new EventLogItemsResponse(
                page,
                pageSize,
                totalItems,
                items.Select(c => new EventLogItemDto(
                    c.Id,
                    c.InfoBaseId,
                    c.InfoBaseName,
                    c.Level,
                    c.Date,
                    c.ApplicationName,
                    c.Event,
                    c.User,
                    c.UserName,
                    c.Computer,
                    c.Metadata,
                    c.MetadataPresentation,
                    c.Comment,
                    c.Data,
                    c.DataPresentation,
                    c.TransactionStatus,
                    c.TransactionDateTime,
                    c.TransactionID,
                    c.Connection,
                    c.Session,
                    c.ServerName,
                    c.Port,
                    c.SyncPort,
                    c.SessionDataSeparation,
                    c.SessionDataSeparationPresentation)).ToList(),
                infoBases));
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    private async Task<Common.EventLog.IEventLogRepository> CreateRepository(CancellationToken cancellationToken)
    {
        var settings = await dbContext.EventLogSettings
            .AsNoTracking()
            .Include(c => c.Dbms)
            .Include(c => c.Credentials)
            .OrderBy(c => c.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (settings == null)
            throw new Exception("Не установлены настройки журнала регистрации");

        var settingsDto = mapper.Map<EventLogSettingsDto>(settings);
        return settingsDto.GetDbContext();
    }

    private static string PrepareFilter(
        IReadOnlyCollection<string> selectedInfoBaseInternalIds,
        DateTime? startDate,
        DateTime? endDate,
        IReadOnlyCollection<string>? eventTypes,
        string? rawFilter)
    {
        var infoBasesFilter = selectedInfoBaseInternalIds.Count switch
        {
            0 => string.Empty,
            _ => $"InfoBaseId IN [{string.Join(',', selectedInfoBaseInternalIds.Select(c => $"'{EscapeValue(c)}'"))}]"
        };

        var periodFilter = string.Empty;

        if (startDate.HasValue && endDate.HasValue)
        {
            periodFilter =
                $"Date BETWEEN toDateTime64('{ClickHouseHelper.SerializeDateTime(startDate.Value.Date, false)}', 6, 'UTC') AND toDateTime64('{ClickHouseHelper.SerializeDateTime(endDate.Value.Date.AddDays(1).AddTicks(-1), false)}', 6, 'UTC')";
        }
        else if (startDate.HasValue)
        {
            periodFilter =
                $"Date >= toDateTime64('{ClickHouseHelper.SerializeDateTime(startDate.Value.Date, false)}', 6, 'UTC')";
        }
        else if (endDate.HasValue)
        {
            periodFilter =
                $"Date <= toDateTime64('{ClickHouseHelper.SerializeDateTime(endDate.Value.Date.AddDays(1).AddTicks(-1), false)}', 6, 'UTC')";
        }

        var eventTypesFilter = eventTypes is { Count: > 0 }
            ? $"Event IN [{string.Join(',', eventTypes.Select(c => $"'{EscapeValue(c)}'"))}]"
            : string.Empty;

        var filters = new List<string>
        {
            infoBasesFilter,
            periodFilter,
            eventTypesFilter,
            rawFilter?.Trim() ?? string.Empty
        }.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();

        return string.Join(" AND ", filters);
    }

    private static string EscapeValue(string value)
        => value.Replace("'", "''");

    public sealed record EventLogLookupsResponse(
        IReadOnlyList<EventLogInfoBaseItem> InfoBases,
        IReadOnlyList<string> EventTypes);

    public sealed record EventLogInfoBaseItem(
        Guid Id,
        string InternalId,
        string Name);

    public sealed record EventLogItemsResponse(
        int Page,
        int PageSize,
        int TotalItems,
        IReadOnlyList<EventLogItemDto> Items,
        IReadOnlyList<EventLogInfoBaseItem> InfoBases);

    public sealed record EventLogItemDto(
        Guid Id,
        string InfoBaseId,
        string InfoBaseName,
        string Level,
        DateTime Date,
        string ApplicationName,
        string Event,
        string User,
        string UserName,
        string Computer,
        string Metadata,
        string MetadataPresentation,
        string Comment,
        string Data,
        string DataPresentation,
        string TransactionStatus,
        DateTime TransactionDateTime,
        long TransactionId,
        string Connection,
        string Session,
        string ServerName,
        int Port,
        int SyncPort,
        string SessionDataSeparation,
        string SessionDataSeparationPresentation);
}
