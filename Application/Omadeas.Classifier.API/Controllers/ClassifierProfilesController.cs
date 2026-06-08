using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Interfaces;

namespace Omadeas.Classifier.API.Controllers;

/// <summary>
/// CRUD for classifier profiles (named bundles of classifiers). Profiles are soft-retired,
/// never hard-deleted (spec §13 #1): DELETE retires and a separate endpoint reactivates.
/// </summary>
[ApiController]
[Authorize]
[Route("api/classifiers/profiles")]
public class ClassifierProfilesController : ControllerBase
{
    private readonly IClassifierProfileService _profileService;
    private readonly ILogger<ClassifierProfilesController> _logger;

    public ClassifierProfilesController(IClassifierProfileService profileService, ILogger<ClassifierProfilesController> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }

    // NOTE: authorization policies are deferred (see Program.AddAuthentication).

    /// <summary>Gets profiles, optionally filtered by company, source and active state.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAllClassifierProfiles(
        [FromQuery] Guid? companyId,
        [FromQuery] string? source,
        [FromQuery] bool? isActive)
    {
        IEnumerable<ClassifierProfileDto> profiles = await _profileService.GetAllClassifierProfilesAsync(companyId, source, isActive);
        return Ok(profiles);
    }

    /// <summary>Gets a single profile by id.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetClassifierProfileById([FromRoute] Guid id)
    {
        ClassifierProfileDto profile = await _profileService.GetClassifierProfileByIdAsync(id);
        return Ok(profile);
    }

    /// <summary>Creates a new profile.</summary>
    [HttpPost]
    public async Task<IActionResult> AddClassifierProfile([FromBody] ClassifierProfileDto profileDto)
    {
        if (profileDto == null)
            return BadRequest("Profile data is required.");

        _logger.LogInformation("Adding profile {Code} for company {CompanyId}", profileDto.Code, profileDto.CompanyId);
        ClassifierProfileDto created = await _profileService.AddClassifierProfileAsync(profileDto);
        return CreatedAtAction(nameof(GetClassifierProfileById), new { id = created.Id }, created);
    }

    /// <summary>Updates an existing profile's editable fields (name, description, order, scope_default).</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClassifierProfile([FromRoute] Guid id, [FromBody] ClassifierProfileDto profileDto)
    {
        if (profileDto == null || profileDto.Id != id)
            return BadRequest("Profile id in the route and body must match.");

        _logger.LogInformation("Updating profile {Id}", id);
        await _profileService.UpdateClassifierProfileAsync(profileDto);
        return NoContent();
    }

    /// <summary>Soft-retires a profile (sets is_active=false).</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> RetireClassifierProfile([FromRoute] Guid id)
    {
        _logger.LogInformation("Retiring profile {Id}", id);
        await _profileService.RetireClassifierProfileAsync(id);
        return NoContent();
    }

    /// <summary>Reactivates a previously retired profile.</summary>
    [HttpPost("{id}/reactivate")]
    public async Task<IActionResult> ReactivateClassifierProfile([FromRoute] Guid id)
    {
        _logger.LogInformation("Reactivating profile {Id}", id);
        await _profileService.ReactivateClassifierProfileAsync(id);
        return NoContent();
    }

    /// <summary>Gets the profile's definition history (most recent first), including member changes.</summary>
    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetClassifierProfileHistory([FromRoute] Guid id)
    {
        IEnumerable<ClassifierProfileHistoryDto> history = await _profileService.GetClassifierProfileHistoryAsync(id);
        return Ok(history);
    }
}
