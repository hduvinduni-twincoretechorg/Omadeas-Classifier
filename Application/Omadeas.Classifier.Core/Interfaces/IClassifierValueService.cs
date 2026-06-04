using Omadeas.Classifier.Core.DTOs;

namespace Omadeas.Classifier.Core.Interfaces;

public interface IClassifierValueService
{
    /// <summary>Returns the values of a classifier (404 if the classifier is missing).</summary>
    Task<IEnumerable<ClassifierValueDto>> GetClassifierValuesAsync(Guid classifierId, bool? isActive);

    /// <summary>Returns a single value by id. Throws NotFoundException if missing.</summary>
    Task<ClassifierValueDto> GetClassifierValueByIdAsync(Guid classifierId, Guid id);

    /// <summary>Creates a value (validated + uniqueness-checked) and writes a 'value_added' history row.</summary>
    Task<ClassifierValueDto> AddClassifierValueAsync(ClassifierValueDto value);

    /// <summary>Updates a value's editable fields and writes a 'value_edited' history row if anything changed.</summary>
    Task UpdateClassifierValueAsync(ClassifierValueDto value);

    /// <summary>Soft-retires a value (is_active=false) and writes a 'value_retired' history row.</summary>
    Task RetireClassifierValueAsync(Guid classifierId, Guid id);

    /// <summary>Reactivates a value (is_active=true) and writes a 'value_edited' history row.</summary>
    Task ReactivateClassifierValueAsync(Guid classifierId, Guid id);
}
