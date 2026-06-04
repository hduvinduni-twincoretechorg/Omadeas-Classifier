using Microsoft.AspNetCore.Mvc;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Interfaces;

namespace Omadeas.Classifier.API.Controllers;

/// <summary>
/// CRUD for classifier definitions. Classifiers are soft-retired, never hard-deleted
/// (spec §13 #1): DELETE retires (is_active=false) and a separate endpoint reactivates.
/// </summary>
[ApiController]
[Route("api/classifiers")]
public class ClassifiersController : ControllerBase
{
    private readonly IClassifierService _classifierService;
    private readonly ILogger<ClassifiersController> _logger;

    public ClassifiersController(IClassifierService classifierService, ILogger<ClassifiersController> logger)
    {
        _classifierService = classifierService;
        _logger = logger;
    }

    // NOTE: authorization policies are deferred (see Program.AddAuthentication).
    // Tenants must not edit platform classifiers — enforce via [Authorize(Policy = ...)] once defined.

    /// <summary>Gets classifiers, optionally filtered by company, source and active state.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAllClassifiers(
        [FromQuery] Guid? companyId,
        [FromQuery] string? source,
        [FromQuery] bool? isActive)
    {
        _logger.LogInformation("Fetching classifiers (companyId={CompanyId}, source={Source}, isActive={IsActive})",
            companyId, source, isActive);
        IEnumerable<ClassifierDto> classifiers = await _classifierService.GetAllClassifiersAsync(companyId, source, isActive);
        return Ok(classifiers);
    }

    /// <summary>Gets a single classifier by id.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetClassifierById([FromRoute] Guid id)
    {
        ClassifierDto classifier = await _classifierService.GetClassifierByIdAsync(id);
        return Ok(classifier);
    }

    /// <summary>Creates a new classifier.</summary>
    [HttpPost]
    public async Task<IActionResult> AddClassifier([FromBody] ClassifierDto classifierDto)
    {
        if (classifierDto == null)
            return BadRequest("Classifier data is required.");

        _logger.LogInformation("Adding classifier {Code} for company {CompanyId}", classifierDto.Code, classifierDto.CompanyId);
        ClassifierDto created = await _classifierService.AddClassifierAsync(classifierDto);
        return CreatedAtAction(nameof(GetClassifierById), new { id = created.Id }, created);
    }

    /// <summary>Updates an existing classifier's editable fields (name, description, order, applies_to_node_types).</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClassifier([FromRoute] Guid id, [FromBody] ClassifierDto classifierDto)
    {
        if (classifierDto == null || classifierDto.Id != id)
            return BadRequest("Classifier id in the route and body must match.");

        _logger.LogInformation("Updating classifier {Id}", id);
        await _classifierService.UpdateClassifierAsync(classifierDto);
        return NoContent();
    }

    /// <summary>Soft-retires a classifier (sets is_active=false).</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> RetireClassifier([FromRoute] Guid id)
    {
        _logger.LogInformation("Retiring classifier {Id}", id);
        await _classifierService.RetireClassifierAsync(id);
        return NoContent();
    }

    /// <summary>Reactivates a previously retired classifier.</summary>
    [HttpPost("{id}/reactivate")]
    public async Task<IActionResult> ReactivateClassifier([FromRoute] Guid id)
    {
        _logger.LogInformation("Reactivating classifier {Id}", id);
        await _classifierService.ReactivateClassifierAsync(id);
        return NoContent();
    }

    /// <summary>Gets the classifier's definition history (most recent first).</summary>
    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetClassifierHistory([FromRoute] Guid id)
    {
        IEnumerable<ClassifierHistoryDto> history = await _classifierService.GetClassifierHistoryAsync(id);
        return Ok(history);
    }
}
