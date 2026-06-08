using Omadeas.Classifier.Core.DTOs;

namespace Omadeas.Classifier.Core.Interfaces;

public interface IClassifierProfileMemberRepository
{
    /// <summary>Returns the members of a profile, ordered by display order.</summary>
    Task<IEnumerable<ClassifierProfileMemberDto>> GetClassifierProfileMembersAsync(Guid profileId);

    /// <summary>Returns a single member by id, or null if not found.</summary>
    Task<ClassifierProfileMemberDto?> GetClassifierProfileMemberByIdAsync(Guid id);

    /// <summary>Inserts a member and returns the created row.</summary>
    Task<ClassifierProfileMemberDto> AddClassifierProfileMemberAsync(ClassifierProfileMemberDto member);

    /// <summary>Updates a member's is_required and order.</summary>
    Task UpdateClassifierProfileMemberAsync(ClassifierProfileMemberDto member);

    /// <summary>Hard-deletes a member (removes the classifier from the profile).</summary>
    Task DeleteClassifierProfileMemberAsync(Guid id);
}
