using Microsoft.AspNetCore.Mvc;

namespace OneSwiss.Server.ApiControllers;

[ApiController]
[Route("api/system")]
public class SystemController : ControllerBase
{
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";

        return Ok(new
        {
            name = "OneSwiss.Server",
            version,
            serverTime = DateTimeOffset.Now
        });
    }
}
