using System.Text.Json;
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
    private readonly IClassifierProfileHistoryRepository _historyRepository;

    public ClassifierProfileMemberService(
        IClassifierProfileMemberRepository memberRepository,
        IClassifierProfileRepository profileRepository,
        IClassifierRepository classifierRepository,
        IClassifierProfileHistoryRepository historyRepository)
    {
        _memberRepository = memberRepository;
        _profileRepository = profileRepository;
        _classifierRepository = classifierRepository;
        _historyRepository = historyRepository;
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
        Guid principalId = member.CreatedBy ?? PlatformConstants.SystemPrincipalId;
        member.CreatedBy = principalId;

        ClassifierProfileMemberDto created = await _memberRepository.AddClassifierProfileMemberAsync(member);

        await WriteHistoryAsync(created.ProfileId, "member_added", principalId,
            $"Classifier {created.ClassifierId} added to profile", JsonSerializer.Serialize(Snapshot(created)));

        return created;
    }

    public async Task UpdateClassifierProfileMemberAsync(ClassifierProfileMemberDto member)
    {
        ClassifierProfileMemberDto existing = await GetExistingAsync(member.ProfileId, member.Id);

        if (member.Order <= 0)
            member.Order = 100;

        bool requiredChanged = existing.IsRequired != member.IsRequired;
        bool orderChanged = existing.Order != member.Order;
        if (!requiredChanged && !orderChanged)
            return;

        Guid principalId = member.UpdatedBy ?? PlatformConstants.SystemPrincipalId;

        // profile_id and classifier_id are immutable.
        existing.IsRequired = member.IsRequired;
        existing.Order = member.Order;
        existing.UpdatedBy = principalId;

        await _memberRepository.UpdateClassifierProfileMemberAsync(existing);

        await WriteHistoryAsync(existing.ProfileId, "member_edited", principalId,
            $"Member {existing.ClassifierId} updated", JsonSerializer.Serialize(Snapshot(existing)));
    }

    public async Task DeleteClassifierProfileMemberAsync(Guid profileId, Guid id)
    {
        ClassifierProfileMemberDto existing = await GetExistingAsync(profileId, id);

        await _memberRepository.DeleteClassifierProfileMemberAsync(id);

        await WriteHistoryAsync(profileId, "member_removed", PlatformConstants.SystemPrincipalId,
            $"Classifier {existing.ClassifierId} removed from profile", JsonSerializer.Serialize(Snapshot(existing)));
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

    private Task WriteHistoryAsync(Guid profileId, string operation, Guid occurredBy, string summary, string? payload)
        => _historyRepository.AddClassifierProfileHistoryAsync(new ClassifierProfileHistoryDto
        {
            ProfileId = profileId,
            Operation = operation,
            OccurredBy = occurredBy,
            OccurredAt = DateTime.UtcNow,
            Summary = summary,
            Payload = payload
        });

    private static object Snapshot(ClassifierProfileMemberDto m)
        => new
        {
            m.Id,
            m.ProfileId,
            m.ClassifierId,
            m.IsRequired,
            m.Order
        };
}
