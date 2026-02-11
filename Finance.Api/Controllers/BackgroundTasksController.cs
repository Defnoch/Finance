using Finance.Application.BackgroundTasks;
using Microsoft.AspNetCore.Mvc;

namespace Finance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BackgroundTasksController : ControllerBase
{
    private readonly IBackgroundTaskService _service;
    public BackgroundTasksController(IBackgroundTaskService service) => _service = service;

    [ProducesResponseType(typeof(IEnumerable<BackgroundTaskConfigDto>), StatusCodes.Status200OK)]
    [HttpGet("config")]
    public async Task<ActionResult<IEnumerable<BackgroundTaskConfigDto>>> GetConfigs()
        => Ok(await _service.GetConfigsAsync());

    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpPatch("config/{id}/lastrun")]
    public async Task<ActionResult> UpdateLastRun(Guid id, [FromBody] DateTime lastRunAt)
    {
        await _service.UpdateLastRunAsync(id, lastRunAt);
        return NoContent();
    }
}
