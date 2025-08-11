using OneScript.Contexts;
using OneSwiss.Agent.Services;
using OneSwiss.Common.DTO;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.Agent.Oscript;

[ContextClass("Сервер", "Server")]
public class OneSwissConnectionWrapper(OneSwissConnection serverConnection)
{
    private bool _started;
    
    [ContextMethod("ОтправитьУведомление", "SendNotification")]
    public void SendCustomNotification(string key, string message)
    {
        StartIfNeeds();
        
        serverConnection.Send(
            MessageType.QueueNotificationRequest,
            new NotificationDto
            {
                Key = key,
                Message = message
            },
            CancellationToken.None).Wait();
    }

    private void StartIfNeeds()
    {
        if (_started) 
            return;
        
        _started = true;
        serverConnection.Start().Wait();
    }
}