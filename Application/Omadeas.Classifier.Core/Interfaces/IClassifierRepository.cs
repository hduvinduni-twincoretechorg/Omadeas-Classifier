using Omadeas.Classifier.Core.DTOs;

namespace Omadeas.Classifier.Core.Interfaces;

public interface IClassifierRepository
{
    /// <summary>Returns classifiers, optionally filtered by company, source and active state.</summary>
    Task<IEnumerable<ClassifierDto>> GetAllClassifiersAsync(Guid? companyId, string? source, bool? isActive);

    /// <summary>Returns a single classifier by id, or null if not found.</summary>
    Task<ClassifierDto?> GetClassifierByIdAsync(Guid id);

    /// <summary>Inserts a new classifier and returns the created row.</summary>
    Task<ClassifierDto> AddClassifierAsync(ClassifierDto classifier);

    /// <summary>Updates the mutable fields (name, description, order, applies_to_node_types).</summary>
    Task UpdateClassifierAsync(ClassifierDto classifier);

    /// <summary>Soft-retires or reactivates a classifier.</summary>
    Task SetClassifierActiveAsync(Guid id, bool isActive, Guid? updatedBy);

    /// <summary>Returns classifiers in the company whose code OR name matches (uniqueness pre-check).</summary>
    Task<IEnumerable<ClassifierDto>> FindClassifiersByCodeOrNameAsync(Guid companyId, string code, string name);
}
