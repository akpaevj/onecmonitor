using OneScript.Contexts;
using OneSwiss.V8.Platform.Services;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.OneScript.Oscript;

[ContextClass("СлужбаV8", "V8Service")]
public class V8ServiceWrapper(V8Service item) : AutoContext<V8ServiceWrapper>
{
    [ContextProperty("Имя", "Name", CanWrite = false)]
    public string Name => item.Name;
    [ContextProperty("Запущена", "IsActive", CanWrite = false)]
    public bool IsActive => item.IsActive;
}