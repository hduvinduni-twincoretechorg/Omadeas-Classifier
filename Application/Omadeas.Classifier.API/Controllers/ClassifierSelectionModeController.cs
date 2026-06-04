using Microsoft.AspNetCore.Mvc;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Interfaces;

namespace Omadeas.Classifier.API.Controllers;

/// <summary>
/// CRUD for the classifier selection-mode lookup (SINGLE_SELECT, MULTIPLE_SELECT,
/// HIERARCHICAL). Platform-seeded; write access is governance-restricted (auth deferred).
/// </summary>
[ApiController]
[Route("api/selection-modes")]
public class ClassifierSelectionModeController : ControllerBase
{
    private readonly IClassifierSelectionModeService _selectionModeService;
    private readonly ILogger<ClassifierSelectionModeController> _logger;

    public ClassifierSelectionModeController(
        IClassifierSelectionModeService selectionModeService,
        ILogger<ClassifierSelectionModeController> logger)
    {
        _selectionModeService = selectionModeService;
        _logger = logger;
    }

    // NOTE: authorization policies are deferred (see Program.AddAuthentication).
    // Writes here should be platform-team only — add [Authorize(Policy = ...)] once defined.

    /// <summary>Gets all selection modes. Optionally filter by active state.</summary>
    /// <param name="isActive">(Optional) filter by active state; omit to return all.</param>
    [HttpGet]
    public async Task<IActionResult> GetAllClassifierSelectionModes([FromQuery] bool? isActive)
    {
        _logger.LogInformation("Fetching classifier selection modes (isActive={IsActive})", isActive);
        IEnumerable<ClassifierSelectionModeDto> modes =
            await _selectionModeService.GetAllClassifierSelectionModesAsync(isActive);
        return Ok(modes);
    }

    /// <summary>Gets a single selection mode by id.</summary>
    /// <param name="id">The selection mode id.</param>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetClassifierSelectionModeById([FromRoute] Guid id)
    {
        _logger.LogInformation("Fetching selection mode {Id}", id);
        ClassifierSelectionModeDto mode = await _selectionModeService.GetClassifierSelectionModeByIdAsync(id);
        return Ok(mode);
    }

    /// <summary>Creates a new selection mode.</summary>
    /// <param name="selectionModeDto">The selection mode to create.</param>
    [HttpPost]
    public async Task<IActionResult> AddClassifierSelectionMode([FromBody] ClassifierSelectionModeDto selectionModeDto)
    {
        if (selectionModeDto == null)
            return BadRequest("Selection mode data is required.");

        _logger.LogInformation("Adding selection mode {Code}", selectionModeDto.Code);
        ClassifierSelectionModeDto created =
            await _selectionModeService.AddClassifierSelectionModeAsync(selectionModeDto);
        return CreatedAtAction(nameof(GetClassifierSelectionModeById), new { id = created.Id }, created);
    }

    /// <summary>Updates an existing selection mode.</summary>
    /// <param name="id">The selection mode id.</param>
    /// <param name="selectionModeDto">The updated selection mode data.</param>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClassifierSelectionMode(
        [FromRoute] Guid id,
        [FromBody] ClassifierSelectionModeDto selectionModeDto)
    {
        if (selectionModeDto == null || selectionModeDto.Id != id)
            return BadRequest("Selection mode id in the route and body must match.");

        _logger.LogInformation("Updating selection mode {Id}", id);
        await _selectionModeService.UpdateClassifierSelectionModeAsync(selectionModeDto);
        return NoContent();
    }

    /// <summary>Deletes a selection mode.</summary>
    /// <param name="id">The selection mode id.</param>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClassifierSelectionMode([FromRoute] Guid id)
    {
        _logger.LogInformation("Deleting selection mode {Id}", id);
        await _selectionModeService.DeleteClassifierSelectionModeAsync(id);
        return NoContent();
    }
}
