using Omadeas.Classifier.Core.DTOs;

namespace Omadeas.Classifier.Core.Interfaces;

public interface IClassifierValueConstraintService
{
    /// <summary>Returns constraints, optionally filtered by classifier (source OR target) and active state.</summary>
    Task<IEnumerable<ClassifierValueConstraintDto>> GetClassifierValueConstraintsAsync(Guid? classifierId, bool? isActive);

    /// <summary>Returns a single constraint by id. Throws NotFoundException if missing.</summary>
    Task<ClassifierValueConstraintDto> GetClassifierValueConstraintByIdAsync(Guid id);

    /// <summary>Creates a constraint (validated + coherence-checked) and writes a 'constraint_added' history row.</summary>
    Task<ClassifierValueConstraintDto> AddClassifierValueConstraintAsync(ClassifierValueConstraintDto constraint);

    /// <summary>Updates a constraint's type and active state.</summary>
    Task UpdateClassifierValueConstraintAsync(ClassifierValueConstraintDto constraint);

    /// <summary>Hard-deletes a constraint and writes a 'constraint_removed' history row.</summary>
    Task DeleteClassifierValueConstraintAsync(Guid id);
}
