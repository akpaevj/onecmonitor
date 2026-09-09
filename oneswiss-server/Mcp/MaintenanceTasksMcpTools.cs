using System.ComponentModel;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using OneSwiss.Server.Models.MaintenanceTasks;
using OneSwiss.Server.Services;

namespace OneSwiss.Server.Mcp;

[McpServerToolType]
public sealed class MaintenanceTasksMcpTools(AppDbContext dbContext, AgentsConnectionsManager agentsConnectionsManager)
{
    [McpServerTool(Name = "maintenance_tasks_list", ReadOnly = true)]
    [Description("Список задач обслуживания информационных баз 1С (без шаблонов): описание, даты начала/окончания, " +
                  "статус выполнения.")]
    [Authorize(Roles = $"{Roles.ReadMaintenanceTasks},{Roles.WriteMaintenanceTasks}")]
    public async Task<IReadOnlyList<MaintenanceTaskResult>> ListTasks(CancellationToken cancellationToken)
    {
        var tasks = await dbContext.MaintenanceTasks
            .AsNoTracking()
            .Where(t => !t.IsTemplate)
            .OrderByDescending(t => t.StartDateTime)
            .ThenBy(t => t.Description)
            .ToListAsync(cancellationToken);

        return tasks.Select(ToTaskResult).ToList();
    }

    [McpServerTool(Name = "maintenance_tasks_list_templates", ReadOnly = true)]
    [Description("Список шаблонов задач обслуживания информационных баз 1С, доступных для запуска новой задачи.")]
    [Authorize(Roles = $"{Roles.ReadMaintenanceTasks},{Roles.WriteMaintenanceTasks}")]
    public async Task<IReadOnlyList<MaintenanceTaskTemplateResult>> ListTemplates(CancellationToken cancellationToken)
    {
        return await dbContext.MaintenanceTasks
            .AsNoTracking()
            .Where(t => t.IsTemplate)
            .OrderBy(t => t.Description)
            .Select(t => new MaintenanceTaskTemplateResult(t.Id, t.Description))
            .ToListAsync(cancellationToken);
    }

    [McpServerTool(Name = "maintenance_tasks_get", ReadOnly = true)]
    [Description("Информация о конкретной задаче обслуживания по идентификатору.")]
    [Authorize(Roles = $"{Roles.ReadMaintenanceTasks},{Roles.WriteMaintenanceTasks}")]
    public async Task<MaintenanceTaskResult> GetTask(
        [Description("Идентификатор задачи обслуживания (GUID)")]
        Guid id,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.MaintenanceTasks
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == id && !t.IsTemplate, cancellationToken);

        if (task == null)
            throw new McpException("Задача обслуживания не найдена");

        return ToTaskResult(task);
    }

    [McpServerTool(Name = "maintenance_tasks_get_log", ReadOnly = true)]
    [Description("Журнал выполнения задачи обслуживания: общие сообщения и сообщения по каждой информационной базе.")]
    [Authorize(Roles = $"{Roles.ReadMaintenanceTasks},{Roles.WriteMaintenanceTasks}")]
    public async Task<MaintenanceTaskLogResult> GetTaskLog(
        [Description("Идентификатор задачи обслуживания (GUID)")]
        Guid id,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.MaintenanceTasks
            .AsNoTracking()
            .Include(t => t.InfoBases)
            .SingleOrDefaultAsync(t => t.Id == id && !t.IsTemplate, cancellationToken);

        if (task == null)
            throw new McpException("Задача обслуживания не найдена");

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
            .Select(c => new MaintenanceTaskInfoBaseLogResult(
                c.Id,
                c.Name,
                groupedByInfoBase.TryGetValue(c.Id, out var items) ? items : []))
            .ToList();

        return new MaintenanceTaskLogResult(taskLogs, infoBaseLogs);
    }

    [McpServerTool(Name = "maintenance_tasks_start", Destructive = true, Idempotent = false)]
    [Description("Запускает задачу обслуживания на исполнение агентами. НЕОБРАТИМОЕ действие, затрагивающее " +
                  "реальные информационные базы 1С (блокировка соединений, обновление конфигурации, загрузка " +
                  "расширений и т.п. - в зависимости от шагов задачи). Перед запуском инструмент обязательно " +
                  "запрашивает у пользователя явное подтверждение через протокол MCP (elicitation); без " +
                  "подтверждения задача не запускается.")]
    [Authorize(Roles = Roles.WriteMaintenanceTasks)]
    public async Task<MaintenanceTaskStartResult> StartTask(
        McpServer server,
        [Description("Идентификатор задачи обслуживания (GUID)")]
        Guid id,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.MaintenanceTasks
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (task == null || task.IsTemplate)
            throw new McpException("Задача обслуживания не найдена");

        if (task.StartDateTime != DateTime.MinValue)
            throw new McpException("Задача уже была запущена");

        if (server.ClientCapabilities?.Elicitation == null)
            throw new McpException(
                "Клиент MCP не поддерживает запрос подтверждения (elicitation). Запуск задач обслуживания " +
                "требует обязательного подтверждения пользователем, поэтому запуск отклонён.");

        var confirmation = await server.ElicitAsync(new ElicitRequestParams
        {
            Message = $"Подтвердите запуск задачи обслуживания \"{task.Description}\" (ID: {task.Id}). " +
                      "Задача начнёт выполняться на подключённых агентах немедленно и не может быть отменена после старта.",
            RequestedSchema = new ElicitRequestParams.RequestSchema
            {
                Properties = new Dictionary<string, ElicitRequestParams.PrimitiveSchemaDefinition>
                {
                    ["confirm"] = new ElicitRequestParams.BooleanSchema
                    {
                        Description = "Запустить задачу обслуживания?",
                        Default = false
                    }
                },
                Required = ["confirm"]
            }
        }, cancellationToken);

        if (!confirmation.IsAccepted || confirmation.Content == null ||
            !confirmation.Content.TryGetValue("confirm", out var confirmValue) ||
            confirmValue.ValueKind != JsonValueKind.True)
            return new MaintenanceTaskStartResult(false, "Запуск задачи отменён: подтверждение не получено.");

        var currentState = await dbContext.MaintenanceTasks
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (currentState == null || currentState.IsTemplate)
            throw new McpException("Задача обслуживания не найдена");

        if (currentState.StartDateTime != DateTime.MinValue)
            throw new McpException("Задача уже была запущена другим способом, пока ожидалось подтверждение");

        await agentsConnectionsManager.StartMaintenanceTask(id, cancellationToken);

        return new MaintenanceTaskStartResult(true, "Задача обслуживания запущена.");
    }

    private static MaintenanceTaskResult ToTaskResult(MaintenanceTask task)
    {
        var status = task.StartDateTime == DateTime.MinValue
            ? "not_started"
            : task.FinishDateTime == DateTime.MinValue
                ? "running"
                : task.IsFaulted
                    ? "faulted"
                    : "finished";

        return new MaintenanceTaskResult(
            task.Id,
            task.Description,
            task.StartDateTime,
            task.FinishDateTime,
            task.IsFaulted,
            status);
    }

    private static MaintenanceTaskLogItemResult ToLogItem(MaintenanceTaskLogItem item)
    {
        return new MaintenanceTaskLogItemResult(item.TimeStamp, item.Message, item.IsError, item.IsFinish);
    }
}

public sealed record MaintenanceTaskResult(
    Guid Id,
    string Description,
    DateTime StartDateTime,
    DateTime FinishDateTime,
    bool IsFaulted,
    string Status);

public sealed record MaintenanceTaskTemplateResult(Guid Id, string Description);

public sealed record MaintenanceTaskLogItemResult(DateTime TimeStamp, string Message, bool IsError, bool IsFinish);

public sealed record MaintenanceTaskInfoBaseLogResult(
    Guid InfoBaseId,
    string InfoBaseName,
    IReadOnlyList<MaintenanceTaskLogItemResult> Logs);

public sealed record MaintenanceTaskLogResult(
    IReadOnlyList<MaintenanceTaskLogItemResult> TaskLogs,
    IReadOnlyList<MaintenanceTaskInfoBaseLogResult> InfoBaseLogs);

public sealed record MaintenanceTaskStartResult(bool Started, string Message);
