using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Exceptions;
using Omadeas.Classifier.Core.Interfaces;

namespace Omadeas.Classifier.Core.Services;

public class ClassifierSelectionModeService : IClassifierSelectionModeService
{
    private static readonly string[] ValidCodes = { "SINGLE_SELECT", "MULTIPLE_SELECT", "HIERARCHICAL" };

    private readonly IClassifierSelectionModeRepository _selectionModeRepository;

    public ClassifierSelectionModeService(IClassifierSelectionModeRepository selectionModeRepository)
    {
        _selectionModeRepository = selectionModeRepository;
    }

    public async Task<IEnumerable<ClassifierSelectionModeDto>> GetAllClassifierSelectionModesAsync(bool? isActive)
    {
        return await _selectionModeRepository.GetAllClassifierSelectionModesAsync(isActive);
    }

    public async Task<ClassifierSelectionModeDto> GetClassifierSelectionModeByIdAsync(Guid id)
    {
        ClassifierSelectionModeDto? mode = await _selectionModeRepository.GetClassifierSelectionModeByIdAsync(id);
        if (mode == null)
            throw new NotFoundException($"Selection mode with ID {id} was not found.");
        return mode;
    }

    public async Task<ClassifierSelectionModeDto> AddClassifierSelectionModeAsync(ClassifierSelectionModeDto selectionMode)
    {
        ValidateCode(selectionMode.Code);
        if (string.IsNullOrWhiteSpace(selectionMode.Name))
            throw new BadRequestException("Selection mode name is required.");

        IEnumerable<ClassifierSelectionModeDto> existing =
            await _selectionModeRepository.GetAllClassifierSelectionModesAsync(null);
        if (existing.Any(m => string.Equals(m.Code, selectionMode.Code, StringComparison.OrdinalIgnoreCase)))
            throw new BadRequestException($"A selection mode with code '{selectionMode.Code}' already exists.");

        return await _selectionModeRepository.AddClassifierSelectionModeAsync(selectionMode);
    }

    public async Task UpdateClassifierSelectionModeAsync(ClassifierSelectionModeDto selectionMode)
    {
        ClassifierSelectionModeDto? existing =
            await _selectionModeRepository.GetClassifierSelectionModeByIdAsync(selectionMode.Id);
        if (existing == null)
            throw new NotFoundException($"Selection mode with ID {selectionMode.Id} was not found.");

        if (string.IsNullOrWhiteSpace(selectionMode.Name))
            throw new BadRequestException("Selection mode name is required.");

        await _selectionModeRepository.UpdateClassifierSelectionModeAsync(selectionMode);
    }

    public async Task DeleteClassifierSelectionModeAsync(Guid id)
    {
        ClassifierSelectionModeDto? existing = await _selectionModeRepository.GetClassifierSelectionModeByIdAsync(id);
        if (existing == null)
            throw new NotFoundException($"Selection mode with ID {id} was not found.");

        await _selectionModeRepository.DeleteClassifierSelectionModeAsync(id);
    }

    private static void ValidateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || !ValidCodes.Contains(code))
            throw new BadRequestException(
                $"Selection mode code must be one of: {string.Join(", ", ValidCodes)}.");
    }
}
