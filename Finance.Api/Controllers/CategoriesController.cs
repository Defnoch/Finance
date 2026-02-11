using Finance.Application.Categories;
using Microsoft.AspNetCore.Mvc;

namespace Finance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ICategoryQueryService _categoryQueryService;
    private readonly ICategoryAssignmentService _categoryAssignmentService;
    private readonly ICategoryCommandService _categoryCommandService;

    public CategoriesController(
        ICategoryQueryService categoryQueryService,
        ICategoryAssignmentService categoryAssignmentService,
        ICategoryCommandService categoryCommandService)
    {
        _categoryQueryService = categoryQueryService;
        _categoryAssignmentService = categoryAssignmentService;
        _categoryCommandService = categoryCommandService;
    }

    /// <summary>
    /// Haal alle categorieën op.
    /// </summary>
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll(CancellationToken cancellationToken)
    {
        var categories = await _categoryQueryService.GetCategoriesAsync(cancellationToken);
        return Ok(categories);
    }

    /// <summary>
    /// Ken een categorie toe aan een transactie.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpPost("assign")]
    public async Task<IActionResult> Assign([FromBody] AssignCategoryCommand command, CancellationToken cancellationToken)
    {
        await _categoryAssignmentService.AssignCategoryAsync(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Ken meerdere categorieën toe aan transacties (batch).
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPost("batch-assign")]
    public async Task<IActionResult> BatchAssign([FromBody] List<AssignCategoryCommand> commands, CancellationToken cancellationToken)
    {
        foreach (var command in commands)
        {
            await _categoryAssignmentService.AssignCategoryAsync(command, cancellationToken);
        }
        return Ok();
    }

    /// <summary>
    /// Maak een nieuwe categorie aan.
    /// </summary>
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var dto = await _categoryCommandService.AddCategoryAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id = dto.CategoryId }, dto);
    }

    /// <summary>
    /// Verwijder de categorie van een transactie.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpDelete("unassign/{transactionId}")]
    public async Task<IActionResult> Unassign(Guid transactionId, CancellationToken cancellationToken)
    {
        await _categoryAssignmentService.UnassignCategoryAsync(new UnassignCategoryCommand { TransactionId = transactionId }, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Update een bestaande categorie.
    /// </summary>
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpPut("{categoryId}")]
    public async Task<ActionResult<CategoryDto>> Update(Guid categoryId, [FromBody] UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        if (categoryId != command.CategoryId)
            return BadRequest("CategoryId in URL en body komen niet overeen.");
        var updated = await _categoryCommandService.UpdateCategoryAsync(command, cancellationToken);
        if (updated == null)
            return NotFound();
        return Ok(updated);
    }

    /// <summary>
    /// Verwijder een categorie.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpDelete("{categoryId}")]
    public async Task<IActionResult> Delete(Guid categoryId, CancellationToken cancellationToken)
    {
        var deleted = await _categoryCommandService.DeleteCategoryAsync(categoryId, cancellationToken);
        if (!deleted)
            return NotFound();
        return NoContent();
    }
}
