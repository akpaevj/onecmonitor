using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Common.DTO;
using OneSwiss.Common.Services;
using OneSwiss.Common.Storage;
using OneSwiss.Server.Services;

namespace OneSwiss.Server.ApiControllers;

[ApiController]
[Route("api/techlog/settings")]
public class TechLogSettingsController(
    AppDbContext dbContext,
    IMapper mapper,
    AgentsConnectionsManager agentsConnectionsManager,
    TechLogRepositoryManager techLogRepositoryManager) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<TechLogSettingsResponse> Get(CancellationToken cancellationToken)
    {
        var settings = await dbContext.TechLogSettings
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .SingleOrDefaultAsync(cancellationToken);

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

        return new TechLogSettingsResponse(
            new TechLogSettingsItem(
                settings?.Enabled ?? false,
                settings?.DbmsId,
                settings?.DatabaseName ?? string.Empty,
                settings?.Table ?? string.Empty,
                settings?.CredentialsId),
            dbmsItems,
            credentialsItems);
    }

    [HttpPut]
    [Authorize]
    public async Task<ActionResult<TechLogSettingsItem>> Save([FromBody] SaveTechLogSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateRequest(request, cancellationToken);
        if (validationError != null)
            return validationError;

        var settings = await dbContext.TechLogSettings
            .OrderBy(s => s.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (settings == null)
        {
            settings = new Models.TechLogSettings();
            dbContext.TechLogSettings.Add(settings);
        }

        settings.Enabled = request.Enabled;
        settings.DbmsId = request.DbmsId;
        settings.DatabaseName = request.DatabaseName.Trim();
        settings.Table = request.Table.Trim();
        settings.CredentialsId = request.CredentialsId;

        await dbContext.SaveChangesAsync(cancellationToken);

        if (settings.Enabled)
        {
            await InitTechLogTable(settings, cancellationToken);

            var settingsWithIncludes = await dbContext.TechLogSettings
                .AsNoTracking()
                .Include(c => c.Credentials)
                .Include(c => c.Dbms)
                .OrderBy(s => s.Id)
                .SingleAsync(cancellationToken);

            techLogRepositoryManager.UpdateSettings(mapper.Map<TechLogSettingsDto>(settingsWithIncludes));
            await agentsConnectionsManager.RaiseUpdateSettings(cancellationToken);
        }

        return Ok(new TechLogSettingsItem(
            settings.Enabled,
            settings.DbmsId,
            settings.DatabaseName,
            settings.Table,
            settings.CredentialsId));
    }

    private async Task InitTechLogTable(Models.TechLogSettings settings, CancellationToken cancellationToken)
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
        await context.InitTechLogTable(cancellationToken);
    }

    private async Task<ActionResult?> ValidateRequest(SaveTechLogSettingsRequest request, CancellationToken cancellationToken)
    {
        if (!request.Enabled)
            return null;

        if (string.IsNullOrWhiteSpace(request.DatabaseName))
            return BadRequest("Не указано имя базы данных");

        if (string.IsNullOrWhiteSpace(request.Table))
            return BadRequest("Не указана таблица базы данных");

        if (request.DbmsId == null)
            return BadRequest("Не указана СУБД");

        if (request.CredentialsId == null)
            return BadRequest("Не указаны учетные данные");

        var dbmsExists = await dbContext.Dbms
            .AsNoTracking()
            .AnyAsync(c => c.Id == request.DbmsId, cancellationToken);
        if (!dbmsExists)
            return BadRequest("Указана несуществующая СУБД");

        var credentialsExists = await dbContext.Credentials
            .AsNoTracking()
            .AnyAsync(c => c.Id == request.CredentialsId && !c.IsToken, cancellationToken);
        if (!credentialsExists)
            return BadRequest("Указаны несуществующие учетные данные");

        return null;
    }

    public sealed record SaveTechLogSettingsRequest(
        bool Enabled,
        Guid? DbmsId,
        string DatabaseName,
        string Table,
        Guid? CredentialsId);

    public sealed record TechLogSettingsItem(
        bool Enabled,
        Guid? DbmsId,
        string DatabaseName,
        string Table,
        Guid? CredentialsId);

    public sealed record LookupItem(
        Guid Id,
        string Name);

    public sealed record TechLogSettingsResponse(
        TechLogSettingsItem Settings,
        IReadOnlyList<LookupItem> Dbms,
        IReadOnlyList<LookupItem> Credentials);
}
