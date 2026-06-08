using Omadeas.Classifier.Core.DTOs;

namespace Omadeas.Classifier.Core.Interfaces;

public interface IClassifierProfileService
{
    /// <summary>Returns profiles, optionally filtered by company, source and active state.</summary>
    Task<IEnumerable<ClassifierProfileDto>> GetAllClassifierProfilesAsync(Guid? companyId, string? source, bool? isActive);

    /// <summary>Returns a single profile by id. Throws NotFoundException if missing.</summary>
    Task<ClassifierProfileDto> GetClassifierProfileByIdAsync(Guid id);

    /// <summary>Creates a profile (validated + uniqueness-checked) and writes a 'created' history row.</summary>
    Task<ClassifierProfileDto> AddClassifierProfileAsync(ClassifierProfileDto profile);

    /// <summary>Updates a profile's editable fields and writes a history row if anything changed.</summary>
    Task UpdateClassifierProfileAsync(ClassifierProfileDto profile);

    /// <summary>Soft-retires a profile (is_active=false) and writes a 'retired' history row.</summary>
    Task RetireClassifierProfileAsync(Guid id);

    /// <summary>Reactivates a profile (is_active=true) and writes an 'activated' history row.</summary>
    Task ReactivateClassifierProfileAsync(Guid id);

    /// <summary>Returns the profile's definition history, most recent first.</summary>
    Task<IEnumerable<ClassifierProfileHistoryDto>> GetClassifierProfileHistoryAsync(Guid id);
}
