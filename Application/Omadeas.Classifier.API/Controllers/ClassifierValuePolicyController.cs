using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Interfaces;

namespace Omadeas.Classifier.API.Controllers;

/// <summary>
/// The selection policy for a classifier (1:1). A default policy is auto-created with the
/// classifier; this controller reads, (re)creates if missing, and updates it. There is no
/// delete — the policy inherits the classifier's lifecycle (spec §4.6).
/// </summary>
[ApiController]
[Authorize]
[Route("api/classifiers/{classifierId}/policy")]
public class ClassifierValuePolicyController : ControllerBase
{
    private readonly IClassifierValuePolicyService _policyService;
    private readonly ILogger<ClassifierValuePolicyController> _logger;

    public ClassifierValuePolicyController(
        IClassifierValuePolicyService policyService,
        ILogger<ClassifierValuePolicyController> logger)
    {
        _policyService = policyService;
        _logger = logger;
    }

    // NOTE: authorization policies are deferred (see Program.AddAuthentication).

    /// <summary>Gets the policy for a classifier.</summary>
    [HttpGet]
    public async Task<IActionResult> GetClassifierValuePolicy([FromRoute] Guid classifierId)
    {
        ClassifierValuePolicyDto policy = await _policyService.GetClassifierValuePolicyAsync(classifierId);
        return Ok(policy);
    }

    /// <summary>Creates a policy for a classifier that doesn't yet have one.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateClassifierValuePolicy(
        [FromRoute] Guid classifierId,
        [FromBody] ClassifierValuePolicyDto policyDto)
    {
        if (policyDto == null)
            return BadRequest("Policy data is required.");

        _logger.LogInformation("Creating policy for classifier {ClassifierId}", classifierId);
        ClassifierValuePolicyDto created = await _policyService.CreateClassifierValuePolicyAsync(classifierId, policyDto);
        return CreatedAtAction(nameof(GetClassifierValuePolicy), new { classifierId }, created);
    }

    /// <summary>Updates the policy for a classifier.</summary>
    [HttpPut]
    public async Task<IActionResult> UpdateClassifierValuePolicy(
        [FromRoute] Guid classifierId,
        [FromBody] ClassifierValuePolicyDto policyDto)
    {
        if (policyDto == null)
            return BadRequest("Policy data is required.");

        _logger.LogInformation("Updating policy for classifier {ClassifierId}", classifierId);
        await _policyService.UpdateClassifierValuePolicyAsync(classifierId, policyDto);
        return NoContent();
    }
}
