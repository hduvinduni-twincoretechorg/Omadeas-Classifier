using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Interfaces;

namespace Omadeas.Classifier.API.Controllers;

/// <summary>
/// CRUD for inter-value constraints (REQUIRES / PROHIBITS / WARNS). Constraints span two
/// classifiers, so they live at a route alongside (not nested under) a single classifier.
/// Advisory only in v2. Constraints may be hard-deleted (spec §4.6).
/// </summary>
[ApiController]
[Authorize]
[Route("api/classifiers/constraints")]
public class ClassifierValueConstraintsController : ControllerBase
{
    private readonly IClassifierValueConstraintService _constraintService;
    private readonly ILogger<ClassifierValueConstraintsController> _logger;

    public ClassifierValueConstraintsController(
        IClassifierValueConstraintService constraintService,
        ILogger<ClassifierValueConstraintsController> logger)
    {
        _constraintService = constraintService;
        _logger = logger;
    }

    // NOTE: authorization policies are deferred (see Program.AddAuthentication).

    /// <summary>Gets constraints, optionally filtered by classifier (source OR target) and active state.</summary>
    [HttpGet]
    public async Task<IActionResult> GetConstraints([FromQuery] Guid? classifierId, [FromQuery] bool? isActive)
    {
        IEnumerable<ClassifierValueConstraintDto> constraints =
            await _constraintService.GetClassifierValueConstraintsAsync(classifierId, isActive);
        return Ok(constraints);
    }

    /// <summary>Gets a single constraint by id.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetConstraintById([FromRoute] Guid id)
    {
        ClassifierValueConstraintDto constraint = await _constraintService.GetClassifierValueConstraintByIdAsync(id);
        return Ok(constraint);
    }

    /// <summary>Creates a new constraint.</summary>
    [HttpPost]
    public async Task<IActionResult> AddConstraint([FromBody] ClassifierValueConstraintDto constraintDto)
    {
        if (constraintDto == null)
            return BadRequest("Constraint data is required.");

        _logger.LogInformation("Adding {Type} constraint", constraintDto.ConstraintType);
        ClassifierValueConstraintDto created = await _constraintService.AddClassifierValueConstraintAsync(constraintDto);
        return CreatedAtAction(nameof(GetConstraintById), new { id = created.Id }, created);
    }

    /// <summary>Updates a constraint's type and active state.</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateConstraint([FromRoute] Guid id, [FromBody] ClassifierValueConstraintDto constraintDto)
    {
        if (constraintDto == null || constraintDto.Id != id)
            return BadRequest("Constraint id in the route and body must match.");

        _logger.LogInformation("Updating constraint {Id}", id);
        await _constraintService.UpdateClassifierValueConstraintAsync(constraintDto);
        return NoContent();
    }

    /// <summary>Hard-deletes a constraint.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteConstraint([FromRoute] Guid id)
    {
        _logger.LogInformation("Deleting constraint {Id}", id);
        await _constraintService.DeleteClassifierValueConstraintAsync(id);
        return NoContent();
    }
}
