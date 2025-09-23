using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneSwiss.Server.Services.CrServerProxy;

namespace OneSwiss.Server.ApiControllers;

[ApiController]
[Route("cr")]
[AllowAnonymous]
public class ConfigurationRepositoryProxyController(CrServerRequestsHandler requestsHandler) : ControllerBase
{
    [HttpPost("{*path}")]
    [DisableRequestSizeLimit]
    [AllowAnonymous]
    public async Task DesignerCall(string path, CancellationToken cancellationToken)
    {
        await requestsHandler.HandleRequest(HttpContext, path, cancellationToken);
    }
}