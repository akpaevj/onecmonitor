using OneScript.Contexts;
using OneSwiss.Common.DTO.MaintenanceTasks;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.Agent.Oscript;

[GlobalContext(Category = "Методы интеграции с OneSwiss", ManualRegistration = true)]
public class OscriptIntegrationGlobalContext(OscriptIntegrationContext context) : GlobalContextBase<OscriptIntegrationGlobalContext>
{
    [ContextMethod("ПолучитьКонтекстИнтеграцииOneSwiss", "GetOneSwissIntegrationContext")]
    public OscriptIntegrationContext GetContext() => context;
}