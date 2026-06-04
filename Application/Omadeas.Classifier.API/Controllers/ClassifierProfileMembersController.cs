using Microsoft.AspNetCore.Mvc;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Interfaces;

namespace Omadeas.Classifier.API.Controllers;

/// <summary>
/// Manages which classifiers belong to a profile. Members are a junction entity: removal is a
/// hard delete (removes the classifier from the bundle, spec §4.6).
/// </summary>
[ApiController]
[Route("api/profiles/{profileId}/members")]
public class ClassifierProfileMembersController : ControllerBase
{
    private readonly IClassifierProfileMemberService _memberService;
    private readonly ILogger<ClassifierProfileMembersController> _logger;

    public ClassifierProfileMembersController(
        IClassifierProfileMemberService memberService,
        ILogger<ClassifierProfileMembersController> logger)
    {
        _memberService = memberService;
        _logger = logger;
    }

    // NOTE: authorization policies are deferred (see Program.AddAuthentication).

    /// <summary>Gets the members of a profile.</summary>
    [HttpGet]
    public async Task<IActionResult> GetProfileMembers([FromRoute] Guid profileId)
    {
        IEnumerable<ClassifierProfileMemberDto> members = await _memberService.GetClassifierProfileMembersAsync(profileId);
        return Ok(members);
    }

    /// <summary>Gets a single member by id.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProfileMemberById([FromRoute] Guid profileId, [FromRoute] Guid id)
    {
        ClassifierProfileMemberDto member = await _memberService.GetClassifierProfileMemberByIdAsync(profileId, id);
        return Ok(member);
    }

    /// <summary>Adds a classifier to the profile.</summary>
    [HttpPost]
    public async Task<IActionResult> AddProfileMember([FromRoute] Guid profileId, [FromBody] ClassifierProfileMemberDto memberDto)
    {
        if (memberDto == null)
            return BadRequest("Member data is required.");

        memberDto.ProfileId = profileId; // route is authoritative
        _logger.LogInformation("Adding classifier {ClassifierId} to profile {ProfileId}", memberDto.ClassifierId, profileId);
        ClassifierProfileMemberDto created = await _memberService.AddClassifierProfileMemberAsync(memberDto);
        return CreatedAtAction(nameof(GetProfileMemberById), new { profileId, id = created.Id }, created);
    }

    /// <summary>Updates a member's is_required override and order.</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProfileMember(
        [FromRoute] Guid profileId,
        [FromRoute] Guid id,
        [FromBody] ClassifierProfileMemberDto memberDto)
    {
        if (memberDto == null)
            return BadRequest("Member data is required.");

        memberDto.Id = id;
        memberDto.ProfileId = profileId;
        _logger.LogInformation("Updating member {Id} on profile {ProfileId}", id, profileId);
        await _memberService.UpdateClassifierProfileMemberAsync(memberDto);
        return NoContent();
    }

    /// <summary>Removes a classifier from the profile.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProfileMember([FromRoute] Guid profileId, [FromRoute] Guid id)
    {
        _logger.LogInformation("Removing member {Id} from profile {ProfileId}", id, profileId);
        await _memberService.DeleteClassifierProfileMemberAsync(profileId, id);
        return NoContent();
    }
}
