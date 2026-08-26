using OneScript.Contexts;
using OneSwiss.Agent.Services.MaintenanceTasks;
using OneSwiss.Common.DTO;
using OneSwiss.Common.DTO.MaintenanceTasks;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.Agent.Oscript;

// Rac/DesignerAgentClient/OnecV8BatchMode остаются недоступны скрипту - шаг ExecuteOneScript
// не должен напрямую управлять кластером или конфигуратором, это отдельная задача при необходимости.
[ContextClass("КонтекстЗадачиОбслуживания", "MaintenanceTaskContext")]
public class MaintenanceStepContextWrapper(MaintenanceStepContext context) : AutoContext<MaintenanceStepContextWrapper>
{
    // OneScript не умеет маршалить System.Guid как возвращаемое значение - отдаём строкой.
    [ContextProperty("ИдентификаторЗадачи", "TaskId", CanWrite = false)]
    public string TaskId => context.Task.Id.ToString();

    [ContextProperty("ОбщееНазначение", "CommonDestination", CanWrite = false)]
    public bool CommonDestination => context.Task.CommonDestination;

    [ContextProperty("ИнформационнаяБаза", "InfoBase", CanWrite = false)]
    public InfoBaseDto? InfoBase => context.InfoBase;

    [ContextProperty("КодДоступа", "AccessCode", CanWrite = false)]
    public string AccessCode => context.AccessCode;

    [ContextProperty("ИдентификаторШага", "StepId", CanWrite = false)]
    public string StepId => context.Step.StepId.ToString();

    [ContextProperty("ИспользуетсяАгентКонфигуратора", "UseDesignerAgent", CanWrite = false)]
    public bool UseDesignerAgent => context.UseDesignerAgent;

    [ContextMethod("ЗаписатьВЛог", "WriteLog")]
    public void WriteLog(string message, bool isError = false)
    {
        context.Log.Add(new MaintenanceTaskLogItemDto
        {
            Id = Guid.NewGuid(),
            Message = message,
            IsError = isError,
            TimeStamp = DateTime.Now,
            InfoBaseId = context.InfoBase?.Id,
            StepId = context.Step.Id,
            TaskId = context.Task.Id
        });
    }
}
