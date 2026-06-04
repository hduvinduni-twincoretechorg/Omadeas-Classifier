using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Interfaces;

namespace Omadeas.Classifier.Core.Services;

public class ClassifierSelectionModeService : IClassifierSelectionModeService
{
    private readonly IClassifierSelectionModeRepository _selectionModeRepository;

    public ClassifierSelectionModeService(IClassifierSelectionModeRepository selectionModeRepository)
    {
        _selectionModeRepository = selectionModeRepository;
    }

    public async Task<IEnumerable<ClassifierSelectionModeDto>> GetAllClassifierSelectionModesAsync(bool? isActive)
    {
        return await _selectionModeRepository.GetAllClassifierSelectionModesAsync(isActive);
    }
}
