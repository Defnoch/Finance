using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Finance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RulesController : ControllerBase
{
    private readonly ICategorizationRuleRepository _repo;

    public RulesController(ICategorizationRuleRepository repo)
    {
        _repo = repo;
    }

    [ProducesResponseType(typeof(CategorizationRule), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CategorizationRule rule)
    {
        if (rule.Id == Guid.Empty) rule.Id = Guid.NewGuid();
        foreach (var cond in rule.Conditions)
            if (cond.Id == Guid.Empty) cond.Id = Guid.NewGuid();
        await _repo.AddAsync(rule);
        return CreatedAtAction(nameof(GetById), new { id = rule.Id }, rule);
    }

    [ProducesResponseType(typeof(List<CategorizationRule>), StatusCodes.Status200OK)]
    [HttpGet]
    public async Task<ActionResult<List<CategorizationRule>>> GetAll()
    {
        var rules = await _repo.GetAllAsync();
        return Ok(rules);
    }

    [ProducesResponseType(typeof(CategorizationRule), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpGet("{id}")]
    public async Task<ActionResult<CategorizationRule?>> GetById(Guid id)
    {
        var rule = await _repo.GetByIdAsync(id);
        if (rule == null) return NotFound();
        return Ok(rule);
    }

    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CategorizationRule rule)
    {
        if (id != rule.Id) return BadRequest();
        await _repo.UpdateAsync(rule);
        return NoContent();
    }

    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _repo.DeleteAsync(id);
        return NoContent();
    }
}
