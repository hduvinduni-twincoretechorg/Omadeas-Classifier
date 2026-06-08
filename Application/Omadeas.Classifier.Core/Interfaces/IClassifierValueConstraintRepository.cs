using Omadeas.Classifier.Core.DTOs;

namespace Omadeas.Classifier.Core.Interfaces;

public interface IClassifierValueConstraintRepository
{
    /// <summary>Returns constraints, optionally filtered by classifier (source OR target) and active state.</summary>
    Task<IEnumerable<ClassifierValueConstraintDto>> GetClassifierValueConstraintsAsync(Guid? classifierId, bool? isActive);

    /// <summary>Returns a single constraint by id, or null if not found.</summary>
    Task<ClassifierValueConstraintDto?> GetClassifierValueConstraintByIdAsync(Guid id);

    /// <summary>Inserts a constraint and returns the created row.</summary>
    Task<ClassifierValueConstraintDto> AddClassifierValueConstraintAsync(ClassifierValueConstraintDto constraint);

    /// <summary>Updates a constraint's type and active state.</summary>
    Task UpdateClassifierValueConstraintAsync(ClassifierValueConstraintDto constraint);

    /// <summary>Hard-deletes a constraint.</summary>
    Task DeleteClassifierValueConstraintAsync(Guid id);
}
