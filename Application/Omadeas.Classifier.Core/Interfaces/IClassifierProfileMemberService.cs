using Omadeas.Classifier.Core.DTOs;

namespace Omadeas.Classifier.Core.Interfaces;

public interface IClassifierProfileMemberService
{
    /// <summary>Returns the members of a profile (404 if the profile is missing).</summary>
    Task<IEnumerable<ClassifierProfileMemberDto>> GetClassifierProfileMembersAsync(Guid profileId);

    /// <summary>Returns a single member by id. Throws NotFoundException if missing.</summary>
    Task<ClassifierProfileMemberDto> GetClassifierProfileMemberByIdAsync(Guid profileId, Guid id);

    /// <summary>Adds a classifier to a profile (validated + uniqueness-checked).</summary>
    Task<ClassifierProfileMemberDto> AddClassifierProfileMemberAsync(ClassifierProfileMemberDto member);

    /// <summary>Updates a member's is_required and order.</summary>
    Task UpdateClassifierProfileMemberAsync(ClassifierProfileMemberDto member);

    /// <summary>Removes a classifier from a profile (hard delete).</summary>
    Task DeleteClassifierProfileMemberAsync(Guid profileId, Guid id);
}
