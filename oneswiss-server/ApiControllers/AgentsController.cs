using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneSwiss.Server.Attributes;
using OneSwiss.Server.Services;

namespace OneSwiss.Server.ApiControllers;

[AllowAnonymous]
[ApiController]
[Route("webhooks")]
public class WebHooksController : ControllerBase
{
    [HttpPost]
    public async Task HandleWebHook()
    {
        using var reader = new StreamReader(Request.Body);
        var requestData = await reader.ReadToEndAsync();
        var a = 1;
    }
}

public class AgentsController(AgentsConnectionsManager connectionsManager)
    : ControllerBase
{
    [Route("ws/agents")]
    [Authorize(Policy = "AgentsAuthenticationPolicy")]
    public async Task AcceptAgent()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            var tcs = new TaskCompletionSource();
            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            connectionsManager.AcceptAgent(webSocket, tcs);

            await tcs.Task;
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}