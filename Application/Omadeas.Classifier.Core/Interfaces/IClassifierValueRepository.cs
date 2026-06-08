using Omadeas.Classifier.Core.DTOs;

namespace Omadeas.Classifier.Core.Interfaces;

public interface IClassifierValueRepository
{
    /// <summary>Returns the values of a classifier, optionally filtered by active state.</summary>
    Task<IEnumerable<ClassifierValueDto>> GetClassifierValuesByClassifierAsync(Guid classifierId, bool? isActive);

    /// <summary>Returns a single value by id, or null if not found.</summary>
    Task<ClassifierValueDto?> GetClassifierValueByIdAsync(Guid id);

    /// <summary>Inserts a new value and returns the created row.</summary>
    Task<ClassifierValueDto> AddClassifierValueAsync(ClassifierValueDto value);

    /// <summary>Updates the mutable fields (name, description, order, colour, icon).</summary>
    Task UpdateClassifierValueAsync(ClassifierValueDto value);

    /// <summary>Soft-retires or reactivates a value.</summary>
    Task SetClassifierValueActiveAsync(Guid id, bool isActive, Guid? updatedBy);

    /// <summary>Returns values in the classifier whose code OR name matches (uniqueness pre-check).</summary>
    Task<IEnumerable<ClassifierValueDto>> FindClassifierValuesByCodeOrNameAsync(Guid classifierId, string code, string name);
}
