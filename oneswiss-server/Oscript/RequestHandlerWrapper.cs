using System.Xml.Linq;
using OneScript.Contexts;
using OneScript.StandardLibrary.Collections;
using OneSwiss.Server.Services.CrServerProxy;
using ScriptEngine.Machine;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.Server.Oscript;

[ContextClass("КонтекстОбработчикаЗапроса", "RequestHandlerContext")]
public class RequestHandlerWrapper(
    CrServerConnection connection,
    HttpContext context,
    string location,
    string repository,
    XDocument requestBody) : AutoContext<RequestHandlerWrapper>
{
    [ContextProperty("АдресПубликации", "Location", CanWrite = false)]
    public string Location => location;
    
    [ContextProperty("Хранилище", "Repository", CanWrite = false)]
    public string Repository => repository;

    [ContextProperty("Комментарий", "Comment", CanWrite = false)]
    public string Comment { get; set; } = string.Empty;
    
    [ContextProperty("ТелоЗапроса", "RequestBody", CanWrite = false)]
    public string RequestBody => requestBody.ToString();

    [ContextProperty("ДополнительныеПараметры", "AdditionalParameters", CanWrite = false)]
    public MapImpl AdditionalParameters { get; set; } = new();

    [ContextMethod("ПередатьЗапрос", "PostRequest")]
    public void PostRequest()
    {
        CrServerRequestsHandler.Send(connection, context, requestBody, CancellationToken.None).Wait();
    }
    
    [ContextMethod("ВызватьИсключение", "RaiseException")]
    public void RaiseException(string message)
    {
        CrServerRequestsHandler.RaiseException(context, message).Wait();
    }

    internal void SetAdditionalParameters(Dictionary<string, string> parameters)
    {
        AdditionalParameters = new MapImpl(parameters
            .Select(c => new KeyAndValueImpl(ValueFactory.Create(c.Key), ValueFactory.Create(c.Value))).ToArray());
    }
}