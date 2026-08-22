using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneScript.StandardLibrary.Collections;
using OneSwiss.Server.Services.CrServerProxy;
using ScriptEngine.Machine;

namespace OneSwiss.Server.ApiControllers;

[ApiController]
[Route("api/crserverproxy")]
[Authorize]
public class CrServerProxyController(AppDbContext dbContext, CrServerRequestsHandler requestsHandler) : ControllerBase
{
    [HttpGet("settings")]
    public async Task<CrServerProxySettingsItem> GetSettings(CancellationToken cancellationToken)
    {
        var settings = await dbContext.CrServerProxySettings
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return new CrServerProxySettingsItem(settings?.Enabled ?? false);
    }

    [HttpPut("settings")]
    public async Task<ActionResult<CrServerProxySettingsItem>> UpdateSettings(
        [FromBody] CrServerProxySettingsItem request,
        CancellationToken cancellationToken)
    {
        var settings = await dbContext.CrServerProxySettings
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings == null)
        {
            settings = new Models.CrServerProxySettings();
            dbContext.CrServerProxySettings.Add(settings);
        }

        settings.Enabled = request.Enabled;

        await dbContext.SaveChangesAsync(cancellationToken);

        requestsHandler.UpdateSettings();

        return Ok(new CrServerProxySettingsItem(settings.Enabled));
    }

    [HttpGet("locations")]
    public async Task<IReadOnlyList<CrServerProxyLocationItem>> GetLocations(CancellationToken cancellationToken)
    {
        return await GetLocationsList(cancellationToken);
    }

    [HttpPut("locations")]
    public async Task<ActionResult<IReadOnlyList<CrServerProxyLocationItem>>> UpdateLocations(
        [FromBody] IReadOnlyList<UpsertCrServerProxyLocationRequest> request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateLocations(request);
        if (validationError != null)
            return validationError;

        var existing = await dbContext.CrServerProxyLocations
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        var requestedIds = request.Select(r => r.Id).ToHashSet();

        foreach (var toRemove in existing.Values.Where(l => !requestedIds.Contains(l.Id)).ToList())
            dbContext.CrServerProxyLocations.Remove(toRemove);

        foreach (var item in request)
        {
            if (existing.TryGetValue(item.Id, out var entity))
            {
                entity.ConfigurationRepositoryId = item.ConfigurationRepositoryId;
                entity.Location = item.Location.Trim();
            }
            else
            {
                dbContext.CrServerProxyLocations.Add(new Models.CrServerProxyLocation
                {
                    Id = item.Id,
                    ConfigurationRepositoryId = item.ConfigurationRepositoryId,
                    Location = item.Location.Trim()
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        requestsHandler.UpdateLocations();

        return Ok(await GetLocationsList(cancellationToken));
    }

    [HttpGet("middlewares")]
    public async Task<IReadOnlyList<CrServerProxyMiddlewareItem>> GetMiddlewares(CancellationToken cancellationToken)
    {
        return await GetMiddlewaresList(cancellationToken);
    }

    [HttpPut("middlewares")]
    public async Task<ActionResult<IReadOnlyList<CrServerProxyMiddlewareItem>>> UpdateMiddlewares(
        [FromBody] IReadOnlyList<UpsertCrServerProxyMiddlewareRequest> request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateMiddlewares(request);
        if (validationError != null)
            return validationError;

        var existingMiddlewares = await dbContext.CrServerProxyMiddlewares
            .Include(m => m.Arguments)
            .Include(m => m.Locations)
            .ToDictionaryAsync(m => m.Id, cancellationToken);

        var requestedIds = request.Select(r => r.Id).ToHashSet();

        foreach (var toRemove in existingMiddlewares.Values.Where(m => !requestedIds.Contains(m.Id)).ToList())
            dbContext.CrServerProxyMiddlewares.Remove(toRemove);

        var allLocations = await dbContext.CrServerProxyLocations.ToDictionaryAsync(l => l.Id, cancellationToken);

        foreach (var item in request)
        {
            if (!existingMiddlewares.TryGetValue(item.Id, out var entity))
            {
                entity = new Models.CrServerProxyMiddleware { Id = item.Id, Locations = [] };
                dbContext.CrServerProxyMiddlewares.Add(entity);
            }

            entity.DebugMode = item.DebugMode;
            entity.ExecutablePath = item.ExecutablePath.Trim();
            entity.FileId = item.FileId;
            entity.ConnectAll = item.ConnectAll;

            var existingArguments = entity.Arguments.ToDictionary(a => a.Id);
            var requestedArgumentIds = item.Arguments.Select(a => a.Id).ToHashSet();

            foreach (var argumentToRemove in entity.Arguments.Where(a => !requestedArgumentIds.Contains(a.Id)).ToList())
            {
                entity.Arguments.Remove(argumentToRemove);
                dbContext.Remove(argumentToRemove);
            }

            foreach (var argumentRequest in item.Arguments)
            {
                if (existingArguments.TryGetValue(argumentRequest.Id, out var argumentEntity))
                {
                    argumentEntity.Key = argumentRequest.Key.Trim();
                    argumentEntity.Value = argumentRequest.Value ?? string.Empty;
                }
                else
                {
                    entity.Arguments.Add(new Models.ConfigurationRepositoryMiddlewareArgument
                    {
                        Id = argumentRequest.Id,
                        Key = argumentRequest.Key.Trim(),
                        Value = argumentRequest.Value ?? string.Empty
                    });
                }
            }

            entity.Locations.Clear();
            foreach (var locationId in item.LocationIds)
            {
                if (allLocations.TryGetValue(locationId, out var location))
                    entity.Locations.Add(location);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        requestsHandler.UpdateMiddlewares();
        requestsHandler.UpdateLocations();

        return Ok(await GetMiddlewaresList(cancellationToken));
    }

    private async Task<IReadOnlyList<CrServerProxyLocationItem>> GetLocationsList(CancellationToken cancellationToken)
    {
        return await dbContext.CrServerProxyLocations
            .AsNoTracking()
            .OrderBy(l => l.Location)
            .Select(l => new CrServerProxyLocationItem(l.Id, l.ConfigurationRepositoryId, l.Location))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<CrServerProxyMiddlewareItem>> GetMiddlewaresList(CancellationToken cancellationToken)
    {
        var middlewares = await dbContext.CrServerProxyMiddlewares
            .AsNoTracking()
            .Include(m => m.Arguments)
            .Include(m => m.Locations)
            .ToListAsync(cancellationToken);

        return middlewares
            .Select(m => new CrServerProxyMiddlewareItem(
                m.Id,
                m.DebugMode,
                m.ExecutablePath,
                m.FileId,
                m.ConnectAll,
                m.Locations.Select(l => l.Id).ToList(),
                m.Arguments.Select(a => new CrServerProxyMiddlewareArgumentItem(a.Id, a.Key, a.Value)).ToList()))
            .ToList();
    }

    private static ActionResult? ValidateLocations(IReadOnlyList<UpsertCrServerProxyLocationRequest> request)
    {
        for (var index = 0; index < request.Count; index++)
        {
            var item = request[index];

            if (item.ConfigurationRepositoryId == Guid.Empty)
                return new BadRequestObjectResult($"В строке {index + 1} не указано хранилище");

            if (string.IsNullOrWhiteSpace(item.Location))
                return new BadRequestObjectResult($"В строке {index + 1} не указан адрес");

            if (!Uri.TryCreate(new Uri("http://localhost"), item.Location, out _))
                return new BadRequestObjectResult($"Адрес в строке {index + 1} не является валидным относительным путем");
        }

        return null;
    }

    private static ActionResult? ValidateMiddlewares(IReadOnlyList<UpsertCrServerProxyMiddlewareRequest> request)
    {
        for (var index = 0; index < request.Count; index++)
        {
            var item = request[index];

            if (!item.DebugMode && item.FileId == null)
                return new BadRequestObjectResult($"В строке {index + 1} не указан файл скрипта-обработчика");

            if (item.DebugMode)
            {
                if (string.IsNullOrEmpty(item.ExecutablePath))
                    return new BadRequestObjectResult($"В строке {index + 1} не указан путь к файлу скрипта-обработчика");

                if (!Path.Exists(item.ExecutablePath))
                    return new BadRequestObjectResult($"Файл по указанному пути в строке {index + 1} не обнаружен");
            }

            var argsTest = new MapImpl();

            for (var argumentIndex = 0; argumentIndex < item.Arguments.Count; argumentIndex++)
            {
                try
                {
                    argsTest.Insert(ValueFactory.Create(item.Arguments[argumentIndex].Key));
                }
                catch
                {
                    return new BadRequestObjectResult(
                        $"Аргумент {argumentIndex} в строке {index + 1} не является валидным ключом структуры");
                }
            }
        }

        return null;
    }

    public sealed record CrServerProxySettingsItem(bool Enabled);

    public sealed record CrServerProxyLocationItem(
        Guid Id,
        Guid ConfigurationRepositoryId,
        string Location);

    public sealed record UpsertCrServerProxyLocationRequest(
        Guid Id,
        Guid ConfigurationRepositoryId,
        string Location);

    public sealed record CrServerProxyMiddlewareArgumentItem(
        Guid Id,
        string Key,
        string Value);

    public sealed record UpsertCrServerProxyMiddlewareArgumentRequest(
        Guid Id,
        string Key,
        string Value);

    public sealed record CrServerProxyMiddlewareItem(
        Guid Id,
        bool DebugMode,
        string ExecutablePath,
        Guid? FileId,
        bool ConnectAll,
        IReadOnlyList<Guid> LocationIds,
        IReadOnlyList<CrServerProxyMiddlewareArgumentItem> Arguments);

    public sealed record UpsertCrServerProxyMiddlewareRequest(
        Guid Id,
        bool DebugMode,
        string ExecutablePath,
        Guid? FileId,
        bool ConnectAll,
        IReadOnlyList<Guid> LocationIds,
        IReadOnlyList<UpsertCrServerProxyMiddlewareArgumentRequest> Arguments);
}
