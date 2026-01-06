using Microsoft.AspNetCore.Mvc;

namespace HelixScheduler.WebApi.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult Get()
    {
        var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";
        return Ok(new
        {
            status = "ok",
            utc = DateTime.UtcNow,
            version
        });
    }
}

