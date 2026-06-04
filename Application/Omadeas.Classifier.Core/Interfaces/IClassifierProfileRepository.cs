using Omadeas.Classifier.Core.DTOs;

namespace Omadeas.Classifier.Core.Interfaces;

public interface IClassifierProfileRepository
{
    /// <summary>Returns profiles, optionally filtered by company, source and active state.</summary>
    Task<IEnumerable<ClassifierProfileDto>> GetAllClassifierProfilesAsync(Guid? companyId, string? source, bool? isActive);

    /// <summary>Returns a single profile by id, or null if not found.</summary>
    Task<ClassifierProfileDto?> GetClassifierProfileByIdAsync(Guid id);

    /// <summary>Inserts a new profile and returns the created row.</summary>
    Task<ClassifierProfileDto> AddClassifierProfileAsync(ClassifierProfileDto profile);

    /// <summary>Updates the mutable fields (name, description, order, scope_default).</summary>
    Task UpdateClassifierProfileAsync(ClassifierProfileDto profile);

    /// <summary>Soft-retires or reactivates a profile.</summary>
    Task SetClassifierProfileActiveAsync(Guid id, bool isActive, Guid? updatedBy);

    /// <summary>Returns profiles in the company whose code OR name matches (uniqueness pre-check).</summary>
    Task<IEnumerable<ClassifierProfileDto>> FindClassifierProfilesByCodeOrNameAsync(Guid companyId, string code, string name);
}
