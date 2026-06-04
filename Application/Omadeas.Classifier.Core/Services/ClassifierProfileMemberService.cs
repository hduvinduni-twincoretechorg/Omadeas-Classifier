using Omadeas.Classifier.Core.Constants;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Exceptions;
using Omadeas.Classifier.Core.Interfaces;

namespace Omadeas.Classifier.Core.Services;

public class ClassifierProfileMemberService : IClassifierProfileMemberService
{
    private readonly IClassifierProfileMemberRepository _memberRepository;
    private readonly IClassifierProfileRepository _profileRepository;
    private readonly IClassifierRepository _classifierRepository;

    public ClassifierProfileMemberService(
        IClassifierProfileMemberRepository memberRepository,
        IClassifierProfileRepository profileRepository,
        IClassifierRepository classifierRepository)
    {
        _memberRepository = memberRepository;
        _profileRepository = profileRepository;
        _classifierRepository = classifierRepository;
    }

    public async Task<IEnumerable<ClassifierProfileMemberDto>> GetClassifierProfileMembersAsync(Guid profileId)
    {
        await EnsureProfileExistsAsync(profileId);
        return await _memberRepository.GetClassifierProfileMembersAsync(profileId);
    }

    public async Task<ClassifierProfileMemberDto> GetClassifierProfileMemberByIdAsync(Guid profileId, Guid id)
    {
        return await GetExistingAsync(profileId, id);
    }

    public async Task<ClassifierProfileMemberDto> AddClassifierProfileMemberAsync(ClassifierProfileMemberDto member)
    {
        await EnsureProfileExistsAsync(member.ProfileId);

        ClassifierDto? classifier = await _classifierRepository.GetClassifierByIdAsync(member.ClassifierId);
        if (classifier == null)
            throw new BadRequestException($"Classifier {member.ClassifierId} was not found.");

        IEnumerable<ClassifierProfileMemberDto> existing = await _memberRepository.GetClassifierProfileMembersAsync(member.ProfileId);
        if (existing.Any(m => m.ClassifierId == member.ClassifierId))
            throw new BadRequestException("That classifier is already a member of this profile.");

        if (member.Order <= 0)
            member.Order = 100;
        member.CreatedBy ??= PlatformConstants.SystemPrincipalId;

        return await _memberRepository.AddClassifierProfileMemberAsync(member);
    }

    public async Task UpdateClassifierProfileMemberAsync(ClassifierProfileMemberDto member)
    {
        ClassifierProfileMemberDto existing = await GetExistingAsync(member.ProfileId, member.Id);

        if (member.Order <= 0)
            member.Order = 100;

        // profile_id and classifier_id are immutable.
        existing.IsRequired = member.IsRequired;
        existing.Order = member.Order;
        existing.UpdatedBy = member.UpdatedBy ?? PlatformConstants.SystemPrincipalId;

        await _memberRepository.UpdateClassifierProfileMemberAsync(existing);
    }

    public async Task DeleteClassifierProfileMemberAsync(Guid profileId, Guid id)
    {
        await GetExistingAsync(profileId, id);
        await _memberRepository.DeleteClassifierProfileMemberAsync(id);
    }

    // ----- helpers -----

    private async Task EnsureProfileExistsAsync(Guid profileId)
    {
        ClassifierProfileDto? profile = await _profileRepository.GetClassifierProfileByIdAsync(profileId);
        if (profile == null)
            throw new NotFoundException($"Profile with ID {profileId} was not found.");
    }

    private async Task<ClassifierProfileMemberDto> GetExistingAsync(Guid profileId, Guid id)
    {
        ClassifierProfileMemberDto? existing = await _memberRepository.GetClassifierProfileMemberByIdAsync(id);
        if (existing == null || existing.ProfileId != profileId)
            throw new NotFoundException($"Member with ID {id} was not found on profile {profileId}.");
        return existing;
    }
}
