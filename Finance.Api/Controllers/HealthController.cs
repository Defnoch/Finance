using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace Finance.Api.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;

        public HealthController(HealthCheckService healthCheckService)
        {
            _healthCheckService = healthCheckService;
        }

        [HttpGet]
        public async Task<IActionResult> Get() // GET /health
        {
            var report = await _healthCheckService.CheckHealthAsync();
            return MapReport(report);
        }

        [HttpGet("live")]
        public IActionResult Live() // GET /health/live
        {
            // Basic liveness probe - if the process is up we return healthy.
            return Ok(new { status = "Healthy" });
        }

        [HttpGet("ready")]
        public async Task<IActionResult> Ready() // GET /health/ready
        {
            // Readiness probe - run health checks (DB etc.)
            var report = await _healthCheckService.CheckHealthAsync();
            return MapReport(report);
        }

        private IActionResult MapReport(HealthReport report)
        {
            var result = new
            {
                status = report.Status.ToString(),
                totalDuration = report.TotalDuration.TotalMilliseconds,
                entries = report.Entries.ToDictionary(kvp => kvp.Key, kvp => new
                {
                    status = kvp.Value.Status.ToString(),
                    description = kvp.Value.Description,
                    duration = kvp.Value.Duration.TotalMilliseconds
                })
            };

            var payload = JsonSerializer.Serialize(result);
            return new ContentResult
            {
                Content = payload,
                ContentType = "application/json",
                StatusCode = report.Status == HealthStatus.Healthy ? 200 : 503
            };
        }
    }
}
