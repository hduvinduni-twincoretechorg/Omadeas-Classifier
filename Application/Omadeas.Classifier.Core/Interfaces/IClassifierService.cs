using Omadeas.Classifier.Core.DTOs;

namespace Omadeas.Classifier.Core.Interfaces;

public interface IClassifierService
{
    /// <summary>Returns classifiers, optionally filtered by company, source and active state.</summary>
    Task<IEnumerable<ClassifierDto>> GetAllClassifiersAsync(Guid? companyId, string? source, bool? isActive);

    /// <summary>Returns a single classifier by id. Throws NotFoundException if missing.</summary>
    Task<ClassifierDto> GetClassifierByIdAsync(Guid id);

    /// <summary>Creates a classifier (validated + uniqueness-checked) and writes a 'created' history row.</summary>
    Task<ClassifierDto> AddClassifierAsync(ClassifierDto classifier);

    /// <summary>Updates a classifier's editable fields and writes a history row if anything changed.</summary>
    Task UpdateClassifierAsync(ClassifierDto classifier);

    /// <summary>Soft-retires a classifier (is_active=false) and writes a 'retired' history row.</summary>
    Task RetireClassifierAsync(Guid id);

    /// <summary>Reactivates a classifier (is_active=true) and writes an 'activated' history row.</summary>
    Task ReactivateClassifierAsync(Guid id);

    /// <summary>Returns the classifier's definition history, most recent first.</summary>
    Task<IEnumerable<ClassifierHistoryDto>> GetClassifierHistoryAsync(Guid id);
}
