using Microsoft.AspNetCore.Mvc;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Interfaces;

namespace Omadeas.Classifier.API.Controllers;

/// <summary>
/// Read-only access to the classifier selection-mode lookup (SINGLE_SELECT,
/// MULTIPLE_SELECT, HIERARCHICAL). Platform-seeded; not editable via the API.
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
    // Add [Authorize(Policy = ...)] once the access model is defined.

    /// <summary>
    /// Gets all selection modes. Optionally filter by active state.
    /// </summary>
    /// <param name="isActive">(Optional) filter by active state; omit to return all.</param>
    /// <returns>The list of selection modes.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllClassifierSelectionModes([FromQuery] bool? isActive)
    {
        _logger.LogInformation("Fetching classifier selection modes (isActive={IsActive})", isActive);
        IEnumerable<ClassifierSelectionModeDto> modes =
            await _selectionModeService.GetAllClassifierSelectionModesAsync(isActive);
        return Ok(modes);
    }
}
