using Omadeas.Classifier.Core.DTOs;

namespace Omadeas.Classifier.Core.Interfaces;

public interface IClassifierSelectionModeRepository
{
    /// <summary>
    /// Returns all selection modes, optionally filtered by active state.
    /// </summary>
    /// <param name="isActive">(Optional) filter by is_active; null returns all.</param>
    Task<IEnumerable<ClassifierSelectionModeDto>> GetAllClassifierSelectionModesAsync(bool? isActive);
}
