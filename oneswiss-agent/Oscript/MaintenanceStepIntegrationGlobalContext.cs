using OneScript.Contexts;
using OneSwiss.Agent.Services.MaintenanceTasks;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.Agent.Oscript;

[GlobalContext(Category = "Методы интеграции с задачей обслуживания", ManualRegistration = true)]
public class MaintenanceStepIntegrationGlobalContext(MaintenanceStepContext stepContext) : GlobalContextBase<MaintenanceStepIntegrationGlobalContext>
{
    [ContextMethod("ПолучитьКонтекстЗадачиОбслуживания", "GetMaintenanceTaskContext")]
    public MaintenanceStepContext GetContext() => stepContext;
}