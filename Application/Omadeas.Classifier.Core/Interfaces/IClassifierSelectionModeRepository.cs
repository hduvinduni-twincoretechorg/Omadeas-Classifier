using Omadeas.Classifier.Core.DTOs;

namespace Omadeas.Classifier.Core.Interfaces;

public interface IClassifierSelectionModeRepository
{
    /// <summary>
    /// Returns all selection modes, optionally filtered by active state.
    /// </summary>
    /// <param name="isActive">(Optional) filter by is_active; null returns all.</param>
    Task<IEnumerable<ClassifierSelectionModeDto>> GetAllClassifierSelectionModesAsync(bool? isActive);

    /// <summary>Returns a single selection mode by id, or null if not found.</summary>
    Task<ClassifierSelectionModeDto?> GetClassifierSelectionModeByIdAsync(Guid id);

    /// <summary>Inserts a new selection mode and returns the created row.</summary>
    Task<ClassifierSelectionModeDto> AddClassifierSelectionModeAsync(ClassifierSelectionModeDto selectionMode);

    /// <summary>Updates the mutable fields of an existing selection mode.</summary>
    Task UpdateClassifierSelectionModeAsync(ClassifierSelectionModeDto selectionMode);

    /// <summary>Deletes a selection mode by id.</summary>
    Task DeleteClassifierSelectionModeAsync(Guid id);
}
