using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Interfaces;

namespace Omadeas.Classifier.API.Controllers;

/// <summary>
/// CRUD for the values of a classifier. Values are soft-retired, never hard-deleted
/// (spec §13 #1): DELETE retires (is_active=false) and a separate endpoint reactivates.
/// </summary>
[ApiController]
[Authorize]
[Route("api/classifiers/{classifierId}/values")]
public class ClassifierValuesController : ControllerBase
{
    private readonly IClassifierValueService _valueService;
    private readonly ILogger<ClassifierValuesController> _logger;

    public ClassifierValuesController(IClassifierValueService valueService, ILogger<ClassifierValuesController> logger)
    {
        _valueService = valueService;
        _logger = logger;
    }

    // NOTE: authorization policies are deferred (see Program.AddAuthentication).

    /// <summary>Gets the values of a classifier. Optionally filter by active state.</summary>
    [HttpGet]
    public async Task<IActionResult> GetClassifierValues([FromRoute] Guid classifierId, [FromQuery] bool? isActive)
    {
        _logger.LogInformation("Fetching values for classifier {ClassifierId} (isActive={IsActive})", classifierId, isActive);
        IEnumerable<ClassifierValueDto> values = await _valueService.GetClassifierValuesAsync(classifierId, isActive);
        return Ok(values);
    }

    /// <summary>Gets a single value by id.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetClassifierValueById([FromRoute] Guid classifierId, [FromRoute] Guid id)
    {
        ClassifierValueDto value = await _valueService.GetClassifierValueByIdAsync(classifierId, id);
        return Ok(value);
    }

    /// <summary>Creates a new value on the classifier.</summary>
    [HttpPost]
    public async Task<IActionResult> AddClassifierValue([FromRoute] Guid classifierId, [FromBody] ClassifierValueDto valueDto)
    {
        if (valueDto == null)
            return BadRequest("Value data is required.");

        valueDto.ClassifierId = classifierId; // route is authoritative
        _logger.LogInformation("Adding value {Code} to classifier {ClassifierId}", valueDto.Code, classifierId);
        ClassifierValueDto created = await _valueService.AddClassifierValueAsync(valueDto);
        return CreatedAtAction(nameof(GetClassifierValueById), new { classifierId, id = created.Id }, created);
    }

    /// <summary>Updates an existing value's editable fields (name, description, order, colour, icon).</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClassifierValue(
        [FromRoute] Guid classifierId,
        [FromRoute] Guid id,
        [FromBody] ClassifierValueDto valueDto)
    {
        if (valueDto == null)
            return BadRequest("Value data is required.");

        valueDto.Id = id;
        valueDto.ClassifierId = classifierId;
        _logger.LogInformation("Updating value {Id} on classifier {ClassifierId}", id, classifierId);
        await _valueService.UpdateClassifierValueAsync(valueDto);
        return NoContent();
    }

    /// <summary>Soft-retires a value (sets is_active=false).</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> RetireClassifierValue([FromRoute] Guid classifierId, [FromRoute] Guid id)
    {
        _logger.LogInformation("Retiring value {Id} on classifier {ClassifierId}", id, classifierId);
        await _valueService.RetireClassifierValueAsync(classifierId, id);
        return NoContent();
    }

    /// <summary>Reactivates a previously retired value.</summary>
    [HttpPost("{id}/reactivate")]
    public async Task<IActionResult> ReactivateClassifierValue([FromRoute] Guid classifierId, [FromRoute] Guid id)
    {
        _logger.LogInformation("Reactivating value {Id} on classifier {ClassifierId}", id, classifierId);
        await _valueService.ReactivateClassifierValueAsync(classifierId, id);
        return NoContent();
    }
}
