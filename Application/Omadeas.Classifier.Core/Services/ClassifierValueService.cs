using System.Text.Json;
using System.Text.RegularExpressions;
using Omadeas.Classifier.Core.Constants;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Exceptions;
using Omadeas.Classifier.Core.Interfaces;

namespace Omadeas.Classifier.Core.Services;

public class ClassifierValueService : IClassifierValueService
{
    private static readonly Regex CodePattern = new("^[A-Z][A-Z0-9_]+$", RegexOptions.Compiled);
    private static readonly Regex ColourPattern = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);

    private readonly IClassifierValueRepository _valueRepository;
    private readonly IClassifierRepository _classifierRepository;
    private readonly IClassifierHistoryRepository _historyRepository;

    public ClassifierValueService(
        IClassifierValueRepository valueRepository,
        IClassifierRepository classifierRepository,
        IClassifierHistoryRepository historyRepository)
    {
        _valueRepository = valueRepository;
        _classifierRepository = classifierRepository;
        _historyRepository = historyRepository;
    }

    public async Task<IEnumerable<ClassifierValueDto>> GetClassifierValuesAsync(Guid classifierId, bool? isActive)
    {
        await EnsureClassifierExistsAsync(classifierId);
        return await _valueRepository.GetClassifierValuesByClassifierAsync(classifierId, isActive);
    }

    public async Task<ClassifierValueDto> GetClassifierValueByIdAsync(Guid classifierId, Guid id)
    {
        return await GetExistingAsync(classifierId, id);
    }

    public async Task<ClassifierValueDto> AddClassifierValueAsync(ClassifierValueDto value)
    {
        await EnsureClassifierExistsAsync(value.ClassifierId);
        ValidateFields(value);

        if (value.ParentValueId.HasValue)
        {
            ClassifierValueDto? parent = await _valueRepository.GetClassifierValueByIdAsync(value.ParentValueId.Value);
            if (parent == null)
                throw new BadRequestException($"Parent value {value.ParentValueId} was not found.");
            if (parent.ClassifierId != value.ClassifierId)
                throw new BadRequestException("Parent value must belong to the same classifier (R3).");
        }

        if (value.Order <= 0)
            value.Order = 100;

        IEnumerable<ClassifierValueDto> clashes = await _valueRepository.FindClassifierValuesByCodeOrNameAsync(
            value.ClassifierId, value.Code, value.Name);
        if (clashes.Any(v => string.Equals(v.Code, value.Code, StringComparison.OrdinalIgnoreCase)))
            throw new BadRequestException($"A value with code '{value.Code}' already exists in this classifier.");
        if (clashes.Any(v => string.Equals(v.Name, value.Name, StringComparison.OrdinalIgnoreCase)))
            throw new BadRequestException($"A value with name '{value.Name}' already exists in this classifier.");

        Guid principalId = value.CreatedBy ?? PlatformConstants.SystemPrincipalId;
        value.CreatedBy = principalId;

        ClassifierValueDto created = await _valueRepository.AddClassifierValueAsync(value);

        await WriteHistoryAsync(created.ClassifierId, "value_added", principalId,
            $"Value '{created.Name}' added", JsonSerializer.Serialize(Snapshot(created)));

        return created;
    }

    public async Task UpdateClassifierValueAsync(ClassifierValueDto value)
    {
        ClassifierValueDto existing = await GetExistingAsync(value.ClassifierId, value.Id);
        ValidateMutableFields(value);

        // code, classifier_id and parent_value_id are immutable.
        bool nameChanged = !string.Equals(existing.Name, value.Name, StringComparison.Ordinal);
        bool orderChanged = existing.Order != value.Order;
        bool descriptionChanged = !string.Equals(existing.Description, value.Description, StringComparison.Ordinal);
        bool colourChanged = !string.Equals(existing.Colour, value.Colour, StringComparison.Ordinal);
        bool iconChanged = !string.Equals(existing.Icon, value.Icon, StringComparison.Ordinal);

        if (!nameChanged && !orderChanged && !descriptionChanged && !colourChanged && !iconChanged)
            return;

        if (value.Order <= 0)
            value.Order = 100;

        if (nameChanged)
        {
            IEnumerable<ClassifierValueDto> clashes = await _valueRepository.FindClassifierValuesByCodeOrNameAsync(
                existing.ClassifierId, existing.Code, value.Name);
            if (clashes.Any(v => v.Id != existing.Id &&
                                 string.Equals(v.Name, value.Name, StringComparison.OrdinalIgnoreCase)))
                throw new BadRequestException($"A value with name '{value.Name}' already exists in this classifier.");
        }

        Guid principalId = value.UpdatedBy ?? PlatformConstants.SystemPrincipalId;
        value.UpdatedBy = principalId;

        await _valueRepository.UpdateClassifierValueAsync(value);

        string payload = JsonSerializer.Serialize(new { before = Snapshot(existing), after = Snapshot(value) });
        await WriteHistoryAsync(existing.ClassifierId, "value_edited", principalId,
            $"Value '{value.Name}' updated", payload);
    }

    public async Task RetireClassifierValueAsync(Guid classifierId, Guid id)
    {
        ClassifierValueDto existing = await GetExistingAsync(classifierId, id);
        if (!existing.IsActive)
            return;

        await _valueRepository.SetClassifierValueActiveAsync(id, false, PlatformConstants.SystemPrincipalId);
        await WriteHistoryAsync(classifierId, "value_retired", PlatformConstants.SystemPrincipalId,
            $"Value '{existing.Name}' retired", JsonSerializer.Serialize(new { id, isActive = false }));
    }

    public async Task ReactivateClassifierValueAsync(Guid classifierId, Guid id)
    {
        ClassifierValueDto existing = await GetExistingAsync(classifierId, id);
        if (existing.IsActive)
            return;

        await _valueRepository.SetClassifierValueActiveAsync(id, true, PlatformConstants.SystemPrincipalId);
        // No 'value_activated' op exists; record reactivation as a value_edited event.
        await WriteHistoryAsync(classifierId, "value_edited", PlatformConstants.SystemPrincipalId,
            $"Value '{existing.Name}' reactivated", JsonSerializer.Serialize(new { id, isActive = true }));
    }

    // ----- helpers -----

    private async Task EnsureClassifierExistsAsync(Guid classifierId)
    {
        ClassifierDto? classifier = await _classifierRepository.GetClassifierByIdAsync(classifierId);
        if (classifier == null)
            throw new NotFoundException($"Classifier with ID {classifierId} was not found.");
    }

    private async Task<ClassifierValueDto> GetExistingAsync(Guid classifierId, Guid id)
    {
        ClassifierValueDto? existing = await _valueRepository.GetClassifierValueByIdAsync(id);
        if (existing == null || existing.ClassifierId != classifierId)
            throw new NotFoundException($"Value with ID {id} was not found on classifier {classifierId}.");
        return existing;
    }

    private Task WriteHistoryAsync(Guid classifierId, string operation, Guid occurredBy, string summary, string? payload)
        => _historyRepository.AddClassifierHistoryAsync(new ClassifierHistoryDto
        {
            ClassifierId = classifierId,
            Operation = operation,
            OccurredBy = occurredBy,
            OccurredAt = DateTime.UtcNow,
            Summary = summary,
            Payload = payload
        });

    private static void ValidateFields(ClassifierValueDto value)
    {
        if (string.IsNullOrWhiteSpace(value.Code) || value.Code.Length > 50 || !CodePattern.IsMatch(value.Code))
            throw new BadRequestException("Value code is required, must be UPPER_SNAKE_CASE (^[A-Z][A-Z0-9_]+$) and at most 50 characters.");
        ValidateMutableFields(value);
    }

    private static void ValidateMutableFields(ClassifierValueDto value)
    {
        if (string.IsNullOrWhiteSpace(value.Name) || value.Name.Length > 100)
            throw new BadRequestException("Value name is required and must be at most 100 characters.");
        if (value.Description is { Length: > 500 })
            throw new BadRequestException("Value description must be at most 500 characters.");
        if (!string.IsNullOrEmpty(value.Colour) && !ColourPattern.IsMatch(value.Colour))
            throw new BadRequestException("Value colour must be a 6-digit hex string like #ef4444.");
        if (value.Icon is { Length: > 20 })
            throw new BadRequestException("Value icon must be at most 20 characters.");
    }

    private static object Snapshot(ClassifierValueDto v)
        => new
        {
            v.Id,
            v.ClassifierId,
            v.ParentValueId,
            v.Code,
            v.Name,
            v.Description,
            v.Order,
            v.Colour,
            v.Icon,
            v.IsActive
        };
}
