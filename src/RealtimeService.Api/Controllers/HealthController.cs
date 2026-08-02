using Microsoft.AspNetCore.Mvc;

namespace RealtimeService.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok("realtime-service running");
}
