using Microsoft.AspNetCore.Mvc;

namespace Qubriq.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            application = "Qubriq API",
            version = "0.1.0",
            stage = 1,
            timestampUtc = DateTime.UtcNow
        });
    }
}
