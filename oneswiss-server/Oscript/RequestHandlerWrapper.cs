using System.Xml.Linq;
using OneScript.Contexts;
using OneScript.StandardLibrary.Collections;
using OneSwiss.Server.Services.CrServerProxy;
using ScriptEngine.Machine;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.Server.Oscript;

[ContextClass("КонтекстОбработчикаЗапроса", "RequestHandlerContext")]
public class RequestHandlerWrapper(
    CrServerRequestsHandler requestsHandler,
    CrServerConnection connection,
    HttpContext context,
    string location,
    string repository,
    string comment,
    string requestFile,
    ILogger<CrServerRequestsHandler> logger) : AutoContext<RequestHandlerWrapper>
{
    [ContextProperty("АдресПубликации", "Location", CanWrite = false)]
    public string Location => location;
    
    [ContextProperty("Хранилище", "Repository", CanWrite = false)]
    public string Repository => repository;

    [ContextProperty("Комментарий", "Comment", CanWrite = false)]
    public string Comment { get; set; } = comment;

    [ContextProperty("ДополнительныеПараметры", "AdditionalParameters", CanWrite = false)]
    public MapImpl AdditionalParameters { get; set; } = new();

    [ContextMethod("ПередатьЗапрос", "PostRequest")]
    public void PostRequest()
    {
        HandleAggregateException(() => requestsHandler.Send(repository, connection, context, requestFile, CancellationToken.None).Wait(),
            "Ошибка отправки запроса серверу хранилищ");
    }
    
    [ContextMethod("ВызватьИсключение", "RaiseException")]
    public void RaiseException(string message)
    {
        HandleAggregateException(() => CrServerRequestsHandler.RaiseException(context, message).Wait(),
            "Ошибка отправки исключения конфигуратору");

    }

    private void HandleAggregateException(Action action, string errorText)
    {
        try
        {
            action.Invoke();
        }
        catch (AggregateException e)
        {
            foreach (var eInnerException in e.InnerExceptions)
                logger.LogError(eInnerException, errorText);

            throw;
        }
    }

    internal void SetAdditionalParameters(Dictionary<string, string> parameters)
    {
        AdditionalParameters = new MapImpl(parameters
            .Select(c => new KeyAndValueImpl(ValueFactory.Create(c.Key), ValueFactory.Create(c.Value))).ToArray());
    }
}