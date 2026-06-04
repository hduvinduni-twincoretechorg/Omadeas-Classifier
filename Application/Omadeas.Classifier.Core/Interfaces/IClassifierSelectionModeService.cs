using Omadeas.Classifier.Core.DTOs;

namespace Omadeas.Classifier.Core.Interfaces;

public interface IClassifierSelectionModeService
{
    /// <summary>
    /// Returns all selection modes, optionally filtered by active state.
    /// </summary>
    /// <param name="isActive">(Optional) filter by is_active; null returns all.</param>
    Task<IEnumerable<ClassifierSelectionModeDto>> GetAllClassifierSelectionModesAsync(bool? isActive);

    /// <summary>Returns a single selection mode by id. Throws NotFoundException if missing.</summary>
    Task<ClassifierSelectionModeDto> GetClassifierSelectionModeByIdAsync(Guid id);

    /// <summary>Creates a new selection mode and returns the created row.</summary>
    Task<ClassifierSelectionModeDto> AddClassifierSelectionModeAsync(ClassifierSelectionModeDto selectionMode);

    /// <summary>Updates an existing selection mode. Throws NotFoundException if missing.</summary>
    Task UpdateClassifierSelectionModeAsync(ClassifierSelectionModeDto selectionMode);

    /// <summary>Deletes a selection mode by id. Throws NotFoundException if missing.</summary>
    Task DeleteClassifierSelectionModeAsync(Guid id);
}
