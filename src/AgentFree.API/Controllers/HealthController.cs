using Microsoft.AspNetCore.Mvc;

namespace AgentFree.API.Controllers;

[ApiController]
[Route("")]
public class HealthController : ControllerBase
{
    [HttpGet("health")]
    [HttpGet("health/live")]
    [HttpGet("health/ready")]
    public IActionResult Health()
    {
        return Ok("OK");
    }
}
