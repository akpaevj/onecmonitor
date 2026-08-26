using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Server.Extensions;
using OneSwiss.Server.Models.MaintenanceTasks;
using OneSwiss.Server.Services;

namespace OneSwiss.Server.ApiControllers;

[ApiController]
[Route("api/maintenancetasks")]
public class MaintenanceTasksController(AppDbContext dbContext, AgentsConnectionsManager agentsConnectionsManager) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{Roles.ReadMaintenanceTasks},{Roles.WriteMaintenanceTasks}")]
    public async Task<IReadOnlyList<MaintenanceTaskListItem>> GetList(CancellationToken cancellationToken)
    {
        return await dbContext.MaintenanceTasks
            .AsNoTracking()
            .Where(t => !t.IsTemplate)
            .OrderByDescending(t => t.StartDateTime)
            .ThenBy(t => t.Description)
            .Select(t => new MaintenanceTaskListItem(
                t.Id,
                t.Description,
                t.StartDateTime,
                t.FinishDateTime,
                t.IsFaulted,
                t.IsTemplate))
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{Roles.ReadMaintenanceTasks},{Roles.WriteMaintenanceTasks}")]
    public async Task<ActionResult<MaintenanceTaskListItem>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.MaintenanceTasks
            .AsNoTracking()
            .Where(t => t.Id == id && !t.IsTemplate)
            .Select(t => new MaintenanceTaskListItem(
                t.Id,
                t.Description,
                t.StartDateTime,
                t.FinishDateTime,
                t.IsFaulted,
                t.IsTemplate))
            .SingleOrDefaultAsync(cancellationToken);

        if (item == null)
            return NotFound();

        return Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = Roles.WriteMaintenanceTasks)]
    public async Task<ActionResult<MaintenanceTaskListItem>> Create([FromBody] UpsertMaintenanceTaskRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(request);
        if (validationError != null)
            return validationError;

        var entity = new MaintenanceTask
        {
            Description = request.Description.Trim(),
            StartDateTime = request.StartDateTime,
            FinishDateTime = request.FinishDateTime,
            IsFaulted = request.IsFaulted,
            IsTemplate = false
        };

        dbContext.MaintenanceTasks.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToListItem(entity));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.WriteMaintenanceTasks)]
    public async Task<ActionResult<MaintenanceTaskListItem>> Update(Guid id,
        [FromBody] UpsertMaintenanceTaskRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.MaintenanceTasks.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (entity == null || entity.IsTemplate)
            return NotFound();

        var validationError = ValidateRequest(request);
        if (validationError != null)
            return validationError;

        entity.Description = request.Description.Trim();
        entity.StartDateTime = request.StartDateTime;
        entity.FinishDateTime = request.FinishDateTime;
        entity.IsFaulted = request.IsFaulted;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToListItem(entity));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.WriteMaintenanceTasks)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.MaintenanceTasks
            .Include(t => t.Steps)
            .Include(t => t.Logs)
            .SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (entity == null || entity.IsTemplate)
            return NotFound();

        dbContext.MaintenanceTasks.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/start")]
    [Authorize(Roles = Roles.WriteMaintenanceTasks)]
    public async Task<IActionResult> Start(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.MaintenanceTasks
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (item == null || item.IsTemplate)
            return NotFound();

        if (item.StartDateTime != DateTime.MinValue)
            return BadRequest("Задача уже была запущена");

        await agentsConnectionsManager.StartMaintenanceTask(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/structure")]
    [Authorize(Roles = $"{Roles.ReadMaintenanceTasks},{Roles.WriteMaintenanceTasks}")]
    public async Task<ActionResult<MaintenanceTaskExportDto>> GetStructure(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.MaintenanceTasks
            .AsNoTracking()
            .Include(t => t.InfoBases)
            .IncludeSteps()
            .SingleOrDefaultAsync(t => t.Id == id && !t.IsTemplate, cancellationToken);

        if (item == null)
            return NotFound();

        return Ok(ToTemplateExport(item));
    }

    [HttpPut("{id:guid}/structure")]
    [Authorize(Roles = Roles.WriteMaintenanceTasks)]
    public async Task<ActionResult<MaintenanceTaskExportDto>> UpdateStructure(Guid id,
        [FromBody] JsonElement payload,
        CancellationToken cancellationToken)
    {
        var item = await dbContext.MaintenanceTasks
            .SingleOrDefaultAsync(t => t.Id == id && !t.IsTemplate, cancellationToken);

        if (item == null)
            return NotFound();

        if (!TryParseStructurePayload(payload, out var request))
            return BadRequest("Неверное тело запроса структуры задачи");

        var validationError = ValidateRequest(new UpsertMaintenanceTaskRequest(
            request.Description,
            request.StartDateTime,
            request.FinishDateTime,
            request.IsFaulted));
        if (validationError != null)
            return validationError;

        var normalized = ToTemplateModel(request);

        item.Description = normalized.Description;
        item.StartDateTime = normalized.StartDateTime;
        item.FinishDateTime = normalized.FinishDateTime;
        item.IsFaulted = normalized.IsFaulted;
        item.CommonDestination = normalized.CommonDestination;
        item.StartWhenDiscoverNewConfigVersion = normalized.StartWhenDiscoverNewConfigVersion;

        await dbContext.Entry(item)
            .Collection(t => t.InfoBases)
            .LoadAsync(cancellationToken);

        var requestedInfoBaseIds = (request.InfoBases ?? [])
            .Select(entry => entry.InfoBaseId ?? entry.Id)
            .Where(infoBaseId => infoBaseId.HasValue)
            .Select(infoBaseId => infoBaseId!.Value)
            .Distinct()
            .ToList();

        var requestedInfoBases = requestedInfoBaseIds.Count == 0
            ? []
            : await dbContext.InfoBases
                .Where(infoBase => requestedInfoBaseIds.Contains(infoBase.Id))
                .ToListAsync(cancellationToken);

        item.InfoBases.Clear();
        foreach (var infoBase in requestedInfoBases)
            item.InfoBases.Add(infoBase);

        await dbContext.MaintenanceSteps
            .Where(step => step.MaintenanceTaskId == item.Id)
            .ExecuteDeleteAsync(cancellationToken);

        foreach (var step in normalized.Steps)
        {
            step.MaintenanceTaskId = item.Id;
            dbContext.MaintenanceSteps.Add(step);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await dbContext.MaintenanceTasks
            .AsNoTracking()
            .Include(t => t.InfoBases)
            .IncludeSteps()
            .SingleAsync(t => t.Id == id && !t.IsTemplate, cancellationToken);

        return Ok(ToTemplateExport(updated));
    }

    [HttpGet("lookups")]
    [Authorize(Roles = $"{Roles.ReadMaintenanceTasks},{Roles.WriteMaintenanceTasks}")]
    public async Task<ActionResult<MaintenanceTaskEditorLookupsDto>> GetEditorLookups(CancellationToken cancellationToken)
    {
        return Ok(await BuildEditorLookups(cancellationToken));
    }

    [HttpGet("templates")]
    [Authorize(Roles = $"{Roles.ReadMaintenanceTasks},{Roles.WriteMaintenanceTasks}")]
    public async Task<IReadOnlyList<MaintenanceTaskListItem>> GetTemplates(CancellationToken cancellationToken)
    {
        return await dbContext.MaintenanceTasks
            .AsNoTracking()
            .Where(t => t.IsTemplate)
            .OrderBy(t => t.Description)
            .Select(t => new MaintenanceTaskListItem(
                t.Id,
                t.Description,
                t.StartDateTime,
                t.FinishDateTime,
                t.IsFaulted,
                t.IsTemplate))
            .ToListAsync(cancellationToken);
    }

    [HttpGet("templates/lookup")]
    [Authorize(Roles = $"{Roles.ReadMaintenanceTasks},{Roles.WriteMaintenanceTasks}")]
    public async Task<IReadOnlyList<MaintenanceTaskTemplateLookupItem>> GetTemplatesLookup(CancellationToken cancellationToken)
    {
        return await dbContext.MaintenanceTasks
            .AsNoTracking()
            .Where(t => t.IsTemplate)
            .OrderBy(t => t.Description)
            .Select(t => new MaintenanceTaskTemplateLookupItem(
                t.Id,
                t.Description))
            .ToListAsync(cancellationToken);
    }

    [HttpGet("templates/{id:guid}")]
    [Authorize(Roles = $"{Roles.ReadMaintenanceTasks},{Roles.WriteMaintenanceTasks}")]
    public async Task<ActionResult<MaintenanceTaskListItem>> GetTemplateById(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.MaintenanceTasks
            .AsNoTracking()
            .Where(t => t.Id == id && t.IsTemplate)
            .Select(t => new MaintenanceTaskListItem(
                t.Id,
                t.Description,
                t.StartDateTime,
                t.FinishDateTime,
                t.IsFaulted,
                t.IsTemplate))
            .SingleOrDefaultAsync(cancellationToken);

        if (item == null)
            return NotFound();

        return Ok(item);
    }

    [HttpGet("templates/{id:guid}/structure")]
    [Authorize(Roles = $"{Roles.ReadMaintenanceTasks},{Roles.WriteMaintenanceTasks}")]
    public async Task<ActionResult<MaintenanceTaskExportDto>> GetTemplateStructure(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.MaintenanceTasks
            .AsNoTracking()
            .Include(t => t.InfoBases)
            .IncludeSteps()
            .SingleOrDefaultAsync(t => t.Id == id && t.IsTemplate, cancellationToken);

        if (item == null)
            return NotFound();

        return Ok(ToTemplateExport(item));
    }

    [HttpPost("templates")]
    [Authorize(Roles = Roles.WriteMaintenanceTasks)]
    public async Task<ActionResult<MaintenanceTaskListItem>> CreateTemplate([FromBody] UpsertMaintenanceTaskRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(request);
        if (validationError != null)
            return validationError;

        var entity = new MaintenanceTask
        {
            Description = request.Description.Trim(),
            StartDateTime = request.StartDateTime,
            FinishDateTime = request.FinishDateTime,
            IsFaulted = request.IsFaulted,
            IsTemplate = true
        };

        dbContext.MaintenanceTasks.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetTemplateById), new { id = entity.Id }, ToListItem(entity));
    }

    [HttpPut("templates/{id:guid}")]
    [Authorize(Roles = Roles.WriteMaintenanceTasks)]
    public async Task<ActionResult<MaintenanceTaskListItem>> UpdateTemplate(Guid id,
        [FromBody] UpsertMaintenanceTaskRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.MaintenanceTasks.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (entity == null || !entity.IsTemplate)
            return NotFound();

        var validationError = ValidateRequest(request);
        if (validationError != null)
            return validationError;

        entity.Description = request.Description.Trim();
        entity.StartDateTime = request.StartDateTime;
        entity.FinishDateTime = request.FinishDateTime;
        entity.IsFaulted = request.IsFaulted;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToListItem(entity));
    }

    [HttpDelete("templates/{id:guid}")]
    [Authorize(Roles = Roles.WriteMaintenanceTasks)]
    public async Task<IActionResult> DeleteTemplate(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.MaintenanceTasks
            .Include(t => t.Steps)
            .Include(t => t.Logs)
            .SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (entity == null || !entity.IsTemplate)
            return NotFound();

        dbContext.MaintenanceTasks.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpGet("templates/{id:guid}/export")]
    [Authorize(Roles = $"{Roles.ReadMaintenanceTasks},{Roles.WriteMaintenanceTasks}")]
    public async Task<IActionResult> ExportTemplate(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.MaintenanceTasks
            .AsNoTracking()
            .Include(t => t.InfoBases)
            .IncludeSteps()
            .SingleOrDefaultAsync(t => t.Id == id && t.IsTemplate, cancellationToken);

        if (item == null)
            return NotFound();

        var payload = ToTemplateExport(item);
        var json = JsonSerializer.Serialize(payload, ExportSerializerOptions);
        var contentData = Encoding.UTF8.GetBytes(json);

        return File(contentData, "application/json; charset=utf-8", "task_template.json");
    }

    [HttpPost("templates/import")]
    [Authorize(Roles = Roles.WriteMaintenanceTasks)]
    public async Task<ActionResult<MaintenanceTaskListItem>> ImportTemplate([FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Не выбран файл шаблона");

        MaintenanceTaskExportDto? payload;

        try
        {
            await using var stream = file.OpenReadStream();
            payload = await JsonSerializer.DeserializeAsync<MaintenanceTaskExportDto>(stream, ExportSerializerOptions,
                cancellationToken);
        }
        catch (JsonException)
        {
            return BadRequest("Неверный формат JSON файла шаблона");
        }

        if (payload == null)
            return BadRequest("Неверный формат JSON файла шаблона");

        var validationError = ValidateRequest(new UpsertMaintenanceTaskRequest(
            payload.Description,
            payload.StartDateTime,
            payload.FinishDateTime,
            payload.IsFaulted));
        if (validationError != null)
            return validationError;

        var entity = ToTemplateModel(payload);

        dbContext.MaintenanceTasks.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetTemplateById), new { id = entity.Id }, ToListItem(entity));
    }

    [HttpPut("templates/{id:guid}/structure")]
    [Authorize(Roles = Roles.WriteMaintenanceTasks)]
    public async Task<ActionResult<MaintenanceTaskExportDto>> UpdateTemplateStructure(Guid id,
        [FromBody] JsonElement payload,
        CancellationToken cancellationToken)
    {
        var item = await dbContext.MaintenanceTasks
            .SingleOrDefaultAsync(t => t.Id == id && t.IsTemplate, cancellationToken);

        if (item == null)
            return NotFound();

        if (!TryParseStructurePayload(payload, out var request))
            return BadRequest("Неверное тело запроса структуры шаблона задачи");

        var validationError = ValidateRequest(new UpsertMaintenanceTaskRequest(
            request.Description,
            request.StartDateTime,
            request.FinishDateTime,
            request.IsFaulted));
        if (validationError != null)
            return validationError;

        var normalized = ToTemplateModel(request);

        item.Description = normalized.Description;
        item.StartDateTime = normalized.StartDateTime;
        item.FinishDateTime = normalized.FinishDateTime;
        item.IsFaulted = normalized.IsFaulted;
        item.CommonDestination = normalized.CommonDestination;
        item.StartWhenDiscoverNewConfigVersion = normalized.StartWhenDiscoverNewConfigVersion;

        await dbContext.Entry(item)
            .Collection(t => t.InfoBases)
            .LoadAsync(cancellationToken);

        var requestedInfoBaseIds = (request.InfoBases ?? [])
            .Select(entry => entry.InfoBaseId ?? entry.Id)
            .Where(infoBaseId => infoBaseId.HasValue)
            .Select(infoBaseId => infoBaseId!.Value)
            .Distinct()
            .ToList();

        var requestedInfoBases = requestedInfoBaseIds.Count == 0
            ? []
            : await dbContext.InfoBases
                .Where(infoBase => requestedInfoBaseIds.Contains(infoBase.Id))
                .ToListAsync(cancellationToken);

        item.InfoBases.Clear();
        foreach (var infoBase in requestedInfoBases)
            item.InfoBases.Add(infoBase);

        await dbContext.MaintenanceSteps
            .Where(step => step.MaintenanceTaskId == item.Id)
            .ExecuteDeleteAsync(cancellationToken);

        foreach (var step in normalized.Steps)
        {
            step.MaintenanceTaskId = item.Id;
            dbContext.MaintenanceSteps.Add(step);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await dbContext.MaintenanceTasks
            .AsNoTracking()
            .Include(t => t.InfoBases)
            .IncludeSteps()
            .SingleAsync(t => t.Id == id && t.IsTemplate, cancellationToken);

        return Ok(ToTemplateExport(updated));
    }

    [HttpGet("templates/lookups")]
    [Authorize(Roles = $"{Roles.ReadMaintenanceTasks},{Roles.WriteMaintenanceTasks}")]
    public async Task<ActionResult<MaintenanceTaskEditorLookupsDto>> GetTemplateEditorLookups(CancellationToken cancellationToken)
    {
        return Ok(await BuildEditorLookups(cancellationToken));
    }

    [HttpGet("{id:guid}/log")]
    [Authorize(Roles = $"{Roles.ReadMaintenanceTasks},{Roles.WriteMaintenanceTasks}")]
    public async Task<ActionResult<MaintenanceTaskLogResponse>> GetLog(Guid id, CancellationToken cancellationToken)
    {
        var task = await dbContext.MaintenanceTasks
            .AsNoTracking()
            .Include(t => t.InfoBases)
            .SingleOrDefaultAsync(t => t.Id == id && !t.IsTemplate, cancellationToken);

        if (task == null)
            return NotFound();

        var logs = await dbContext.MaintenanceTaskLogs
            .AsNoTracking()
            .Where(c => c.TaskId == id)
            .OrderBy(c => c.TimeStamp)
            .ToListAsync(cancellationToken);

        var taskLogs = logs
            .Where(c => c.InfoBaseId == null)
            .Select(ToLogItem)
            .ToList();

        var groupedByInfoBase = logs
            .Where(c => c.InfoBaseId != null)
            .GroupBy(c => c.InfoBaseId!.Value)
            .ToDictionary(c => c.Key, c => c.Select(ToLogItem).ToList());

        var infoBaseLogs = task.InfoBases
            .OrderBy(c => c.Name)
            .Select(c => new InfoBaseLogGroup(
                c.Id,
                c.Name,
                groupedByInfoBase.TryGetValue(c.Id, out var items) ? items : []))
            .ToList();

        return Ok(new MaintenanceTaskLogResponse(taskLogs, infoBaseLogs));
    }

    private static readonly JsonSerializerOptions ExportSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
        WriteIndented = true
    };

    private async Task<MaintenanceTaskEditorLookupsDto> BuildEditorLookups(CancellationToken cancellationToken)
    {
        var files = await dbContext.Files
            .AsNoTracking()
            .OrderBy(f => f.Name)
            .ThenBy(f => f.Version)
            .Select(f => new LookupItemDto(f.Id, $"{f.Name} ({f.Version})"))
            .ToListAsync(cancellationToken);

        var credentials = await dbContext.Credentials
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new LookupItemDto(c.Id, c.Name))
            .ToListAsync(cancellationToken);

        var infoBases = await dbContext.InfoBases
            .AsNoTracking()
            .OrderBy(i => i.Name)
            .Select(i => new LookupItemDto(i.Id, i.Name))
            .ToListAsync(cancellationToken);

        var repositories = await dbContext.ConfigRepositories
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ThenBy(r => r.Host)
            .Select(r => new LookupItemDto(r.Id, $"{r.Name} ({r.Host}:{r.Port})"))
            .ToListAsync(cancellationToken);

        return new MaintenanceTaskEditorLookupsDto(files, credentials, infoBases, repositories);
    }

    private static ActionResult? ValidateRequest(UpsertMaintenanceTaskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
            return new BadRequestObjectResult("Не заполнено описание");

        if (request.FinishDateTime != DateTime.MinValue &&
            request.StartDateTime != DateTime.MinValue &&
            request.FinishDateTime < request.StartDateTime)
            return new BadRequestObjectResult("Дата окончания не может быть раньше даты начала");

        return null;
    }

    private static bool TryParseStructurePayload(JsonElement payloadElement, out MaintenanceTaskExportDto payload)
    {
        payload = null!;

        var root = payloadElement;
        if (payloadElement.ValueKind == JsonValueKind.Object &&
            payloadElement.TryGetProperty("payload", out var nestedPayload) &&
            nestedPayload.ValueKind == JsonValueKind.Object)
        {
            root = nestedPayload;
        }

        if (root.ValueKind != JsonValueKind.Object)
            return false;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        jsonOptions.Converters.Add(new JsonStringEnumConverter());

        try
        {
            var parsed = root.Deserialize<MaintenanceTaskExportDto>(jsonOptions);
            if (parsed == null)
                return false;

            payload = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static MaintenanceTaskExportDto ToTemplateExport(MaintenanceTask task)
    {
        var orderedSteps = task.Steps
            .OrderBy(s => s.PositionX)
            .ThenBy(s => s.PositionY)
            .ToList();

        return new MaintenanceTaskExportDto(
            task.Description,
            task.StartDateTime,
            task.FinishDateTime,
            task.IsFaulted,
            task.IsTemplate,
            task.CommonDestination,
            task.StartWhenDiscoverNewConfigVersion,
            task.InfoBases.Select(infoBase => new MaintenanceTaskInfoBaseRefDto(infoBase.Id, infoBase.Id)).ToList(),
            orderedSteps.Select(ToExportStep).ToList());
    }

    private static MaintenanceTaskExportStepDto ToExportStep(MaintenanceStep step)
    {
        return new MaintenanceTaskExportStepDto(
            step.StepId,
            step.Kind,
            step.NodeKind,
            step.PreviousStepId,
            step.LeftStepId,
            step.RightStepId,
            step.PositionX,
            step.PositionY,
            step.CopyInfoBaseStep is null
                ? null
                : new CopyInfoBaseStepExportDto(
                    step.CopyInfoBaseStep.SourceCredentialsId,
                    step.CopyInfoBaseStep.SourceInfoBaseId,
                    step.CopyInfoBaseStep.DestinationCredentialsId,
                    step.CopyInfoBaseStep.DestinationInfoBaseId),
            step.ExecuteOneScriptStep is null
                ? null
                : new ExecuteOneScriptStepExportDto(
                    step.ExecuteOneScriptStep.DebugMode,
                    step.ExecuteOneScriptStep.ExecutablePath,
                    step.ExecuteOneScriptStep.FileId),
            step.StartExternalDataProcessorStep is null
                ? null
                : new StartExternalDataProcessorStepExportDto(
                    step.StartExternalDataProcessorStep.FileId),
            step.UpdateConfigurationStep is null
                ? null
                : new UpdateConfigurationStepExportDto(
                    step.UpdateConfigurationStep.FileId),
            step.LoadExtensionStep is null
                ? null
                : new LoadExtensionStepExportDto(
                    step.LoadExtensionStep.FromConfigRepository,
                    step.LoadExtensionStep.LoadExactVersion,
                    step.LoadExtensionStep.Version,
                    step.LoadExtensionStep.ExtensionName,
                    step.LoadExtensionStep.FileId,
                    step.LoadExtensionStep.BaseConfigurationRepositoryId,
                    step.LoadExtensionStep.ConfigurationRepositoryId),
            step.DeleteExtensionStep is null
                ? null
                : new DeleteExtensionStepExportDto(
                    step.DeleteExtensionStep.ExtensionName),
            step.LoadConfigurationStep is null
                ? null
                : new LoadConfigurationStepExportDto(
                    step.LoadConfigurationStep.FromConfigRepository,
                    step.LoadConfigurationStep.LoadExactVersion,
                    step.LoadConfigurationStep.Version,
                    step.LoadConfigurationStep.FileId,
                    step.LoadConfigurationStep.ConfigurationRepositoryId),
            step.LockConnectionsStep is null
                ? null
                : new LockConnectionsStepExportDto(
                    step.LockConnectionsStep.AccessCode,
                    step.LockConnectionsStep.Message));
    }

    private static MaintenanceTask ToTemplateModel(MaintenanceTaskExportDto payload)
    {
        var taskId = Guid.NewGuid();

        var task = new MaintenanceTask
        {
            Id = taskId,
            Description = payload.Description.Trim(),
            StartDateTime = payload.StartDateTime,
            FinishDateTime = payload.FinishDateTime,
            IsFaulted = payload.IsFaulted,
            IsTemplate = true,
            CommonDestination = payload.CommonDestination,
            StartWhenDiscoverNewConfigVersion = payload.StartWhenDiscoverNewConfigVersion
        };

        var importedSteps = (payload.Steps ?? [])
            .Select(step => ToStepModel(step, taskId))
            .ToList();

        task.Steps = importedSteps;

        return task;
    }

    private static MaintenanceStep ToStepModel(MaintenanceTaskExportStepDto step, Guid taskId)
    {
        return new MaintenanceStep
        {
            Id = Guid.NewGuid(),
            StepId = step.StepId == Guid.Empty ? Guid.NewGuid() : step.StepId,
            MaintenanceTaskId = taskId,
            Kind = step.Kind,
            NodeKind = step.NodeKind,
            PreviousStepId = step.PreviousStepId,
            LeftStepId = step.LeftStepId,
            RightStepId = step.RightStepId,
            PositionX = step.PositionX,
            PositionY = step.PositionY,
            CopyInfoBaseStep = step.CopyInfoBaseStep is null
                ? null
                : new CopyInfoBaseStep
                {
                    SourceCredentialsId = step.CopyInfoBaseStep.SourceCredentialsId,
                    SourceInfoBaseId = step.CopyInfoBaseStep.SourceInfoBaseId,
                    DestinationCredentialsId = step.CopyInfoBaseStep.DestinationCredentialsId,
                    DestinationInfoBaseId = step.CopyInfoBaseStep.DestinationInfoBaseId
                },
            ExecuteOneScriptStep = step.ExecuteOneScriptStep is null
                ? null
                : new ExecuteOneScriptStep
                {
                    DebugMode = step.ExecuteOneScriptStep.DebugMode,
                    ExecutablePath = step.ExecuteOneScriptStep.ExecutablePath ?? string.Empty,
                    FileId = step.ExecuteOneScriptStep.FileId
                },
            StartExternalDataProcessorStep = step.StartExternalDataProcessorStep is null
                ? null
                : new StartExternalDataProcessorStep
                {
                    FileId = step.StartExternalDataProcessorStep.FileId
                },
            UpdateConfigurationStep = step.UpdateConfigurationStep is null
                ? null
                : new UpdateConfigurationStep
                {
                    FileId = step.UpdateConfigurationStep.FileId
                },
            LoadExtensionStep = step.LoadExtensionStep is null
                ? null
                : new LoadExtensionStep
                {
                    FromConfigRepository = step.LoadExtensionStep.FromConfigRepository,
                    LoadExactVersion = step.LoadExtensionStep.LoadExactVersion,
                    Version = step.LoadExtensionStep.Version,
                    ExtensionName = step.LoadExtensionStep.ExtensionName ?? string.Empty,
                    FileId = step.LoadExtensionStep.FileId,
                    BaseConfigurationRepositoryId = step.LoadExtensionStep.BaseConfigurationRepositoryId,
                    ConfigurationRepositoryId = step.LoadExtensionStep.ConfigurationRepositoryId
                },
            DeleteExtensionStep = step.DeleteExtensionStep is null
                ? null
                : new DeleteExtensionStep
                {
                    ExtensionName = step.DeleteExtensionStep.ExtensionName ?? string.Empty
                },
            LoadConfigurationStep = step.LoadConfigurationStep is null
                ? null
                : new LoadConfigurationStep
                {
                    FromConfigRepository = step.LoadConfigurationStep.FromConfigRepository,
                    LoadExactVersion = step.LoadConfigurationStep.LoadExactVersion,
                    Version = step.LoadConfigurationStep.Version,
                    FileId = step.LoadConfigurationStep.FileId,
                    ConfigurationRepositoryId = step.LoadConfigurationStep.ConfigurationRepositoryId
                },
            LockConnectionsStep = step.LockConnectionsStep is null
                ? null
                : new LockConnectionsStep
                {
                    AccessCode = step.LockConnectionsStep.AccessCode ?? string.Empty,
                    Message = step.LockConnectionsStep.Message ?? string.Empty
                }
        };
    }

    private static MaintenanceTaskListItem ToListItem(MaintenanceTask task)
    {
        return new MaintenanceTaskListItem(
            task.Id,
            task.Description,
            task.StartDateTime,
            task.FinishDateTime,
            task.IsFaulted,
            task.IsTemplate);
    }

    private static MaintenanceTaskLogItemDto ToLogItem(MaintenanceTaskLogItem item)
    {
        return new MaintenanceTaskLogItemDto(
            item.TimeStamp,
            item.Message,
            item.IsError,
            item.IsFinish);
    }

    public sealed record UpsertMaintenanceTaskRequest(
        string Description,
        DateTime StartDateTime,
        DateTime FinishDateTime,
        bool IsFaulted);

    public sealed record MaintenanceTaskListItem(
        Guid Id,
        string Description,
        DateTime StartDateTime,
        DateTime FinishDateTime,
        bool IsFaulted,
        bool IsTemplate);

    public sealed record MaintenanceTaskLogResponse(
        IReadOnlyList<MaintenanceTaskLogItemDto> TaskLogs,
        IReadOnlyList<InfoBaseLogGroup> InfoBaseLogs);

    public sealed record MaintenanceTaskTemplateLookupItem(
        Guid Id,
        string Description);

    public sealed record LookupItemDto(
        Guid Id,
        string Name);

    public sealed record MaintenanceTaskEditorLookupsDto(
        IReadOnlyList<LookupItemDto> Files,
        IReadOnlyList<LookupItemDto> Credentials,
        IReadOnlyList<LookupItemDto> InfoBases,
        IReadOnlyList<LookupItemDto> ConfigurationRepositories);

    public sealed record MaintenanceTaskExportDto(
        string Description,
        DateTime StartDateTime,
        DateTime FinishDateTime,
        bool IsFaulted,
        bool IsTemplate,
        bool CommonDestination,
        bool StartWhenDiscoverNewConfigVersion,
        IReadOnlyList<MaintenanceTaskInfoBaseRefDto>? InfoBases,
        IReadOnlyList<MaintenanceTaskExportStepDto> Steps);

    public sealed record MaintenanceTaskInfoBaseRefDto(
        Guid? Id,
        Guid? InfoBaseId);

    public sealed record MaintenanceTaskExportStepDto(
        Guid StepId,
        OneSwiss.Common.Models.MaintenanceTasks.MaintenanceStepKind Kind,
        OneSwiss.Common.Models.MaintenanceTasks.MaintenanceStepNodeKind NodeKind,
        Guid? PreviousStepId,
        Guid? LeftStepId,
        Guid? RightStepId,
        double PositionX,
        double PositionY,
        CopyInfoBaseStepExportDto? CopyInfoBaseStep,
        ExecuteOneScriptStepExportDto? ExecuteOneScriptStep,
        StartExternalDataProcessorStepExportDto? StartExternalDataProcessorStep,
        UpdateConfigurationStepExportDto? UpdateConfigurationStep,
        LoadExtensionStepExportDto? LoadExtensionStep,
        DeleteExtensionStepExportDto? DeleteExtensionStep,
        LoadConfigurationStepExportDto? LoadConfigurationStep,
        LockConnectionsStepExportDto? LockConnectionsStep);

    public sealed record CopyInfoBaseStepExportDto(
        Guid? SourceCredentialsId,
        Guid? SourceInfoBaseId,
        Guid? DestinationCredentialsId,
        Guid? DestinationInfoBaseId);

    public sealed record ExecuteOneScriptStepExportDto(
        bool DebugMode,
        string ExecutablePath,
        Guid? FileId);

    public sealed record StartExternalDataProcessorStepExportDto(
        Guid? FileId);

    public sealed record UpdateConfigurationStepExportDto(
        Guid? FileId);

    public sealed record LoadExtensionStepExportDto(
        bool FromConfigRepository,
        bool LoadExactVersion,
        int Version,
        string ExtensionName,
        Guid? FileId,
        Guid? BaseConfigurationRepositoryId,
        Guid? ConfigurationRepositoryId);

    public sealed record DeleteExtensionStepExportDto(
        string ExtensionName);

    public sealed record LoadConfigurationStepExportDto(
        bool FromConfigRepository,
        bool LoadExactVersion,
        int Version,
        Guid? FileId,
        Guid? ConfigurationRepositoryId);

    public sealed record LockConnectionsStepExportDto(
        string AccessCode,
        string Message);

    public sealed record InfoBaseLogGroup(
        Guid InfoBaseId,
        string InfoBaseName,
        IReadOnlyList<MaintenanceTaskLogItemDto> Logs);

    public sealed record MaintenanceTaskLogItemDto(
        DateTime TimeStamp,
        string Message,
        bool IsError,
        bool IsFinish);
}
