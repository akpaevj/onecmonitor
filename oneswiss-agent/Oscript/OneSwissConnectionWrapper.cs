using OneScript.Contexts;
using OneSwiss.Agent.Services;

namespace OneSwiss.Agent.Oscript;

[ContextClass("Сервер", "Server")]
public class OneSwissConnectionWrapper(
    [FromKeyedServices(OneSwissConnection.CommonKey)]
    OneSwissConnection serverConnection)
{
    private bool _started;

    [ContextMethod("ОтправитьУведомление", "SendNotification")]
    public void SendCustomNotification(string key, string message)
    {
        StartIfNeeds();
        serverConnection.QueueCustomNotification(key, message, CancellationToken.None).Wait();
    }

    private void StartIfNeeds()
    {
        if (_started)
            return;

        _started = true;
        serverConnection.Start().Wait();
    }
}