using Microsoft.AspNetCore.Mvc;
using TrueDope.Api.DTOs;

namespace TrueDope.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public HealthController(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var healthData = new
        {
            Status = "Healthy",
            Checks = new Dictionary<string, string>
            {
                { "api", "Healthy" }
            },
            Version = "2.0.0",
            Environment = _environment.EnvironmentName,
            Timestamp = DateTime.UtcNow
        };

        return Ok(ApiResponse<object>.Ok(healthData));
    }
}
