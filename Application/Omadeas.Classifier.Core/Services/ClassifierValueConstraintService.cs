using System.Text.Json;
using Omadeas.Classifier.Core.Constants;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Exceptions;
using Omadeas.Classifier.Core.Interfaces;

namespace Omadeas.Classifier.Core.Services;

public class ClassifierValueConstraintService : IClassifierValueConstraintService
{
    private static readonly string[] ConstraintTypes = { "REQUIRES", "PROHIBITS", "WARNS" };

    private readonly IClassifierValueConstraintRepository _constraintRepository;
    private readonly IClassifierValueRepository _valueRepository;
    private readonly IClassifierHistoryRepository _historyRepository;

    public ClassifierValueConstraintService(
        IClassifierValueConstraintRepository constraintRepository,
        IClassifierValueRepository valueRepository,
        IClassifierHistoryRepository historyRepository)
    {
        _constraintRepository = constraintRepository;
        _valueRepository = valueRepository;
        _historyRepository = historyRepository;
    }

    public async Task<IEnumerable<ClassifierValueConstraintDto>> GetClassifierValueConstraintsAsync(Guid? classifierId, bool? isActive)
    {
        return await _constraintRepository.GetClassifierValueConstraintsAsync(classifierId, isActive);
    }

    public async Task<ClassifierValueConstraintDto> GetClassifierValueConstraintByIdAsync(Guid id)
    {
        return await GetExistingAsync(id);
    }

    public async Task<ClassifierValueConstraintDto> AddClassifierValueConstraintAsync(ClassifierValueConstraintDto constraint)
    {
        ValidateType(constraint.ConstraintType);

        if (constraint.SourceClassifierId == constraint.TargetClassifierId &&
            constraint.SourceValueId == constraint.TargetValueId)
            throw new BadRequestException("A constraint's source and target value cannot be identical.");

        await EnsureValueBelongsToClassifierAsync(constraint.SourceValueId, constraint.SourceClassifierId, "source");
        await EnsureValueBelongsToClassifierAsync(constraint.TargetValueId, constraint.TargetClassifierId, "target");

        Guid principalId = constraint.CreatedBy ?? PlatformConstants.SystemPrincipalId;
        constraint.CreatedBy = principalId;

        ClassifierValueConstraintDto created = await _constraintRepository.AddClassifierValueConstraintAsync(constraint);

        await WriteHistoryAsync(created.SourceClassifierId, "constraint_added", principalId,
            $"Constraint {created.ConstraintType} added", JsonSerializer.Serialize(Snapshot(created)));

        return created;
    }

    public async Task UpdateClassifierValueConstraintAsync(ClassifierValueConstraintDto constraint)
    {
        ClassifierValueConstraintDto existing = await GetExistingAsync(constraint.Id);
        ValidateType(constraint.ConstraintType);

        // Source/target references are immutable; only type and active state change.
        existing.ConstraintType = constraint.ConstraintType;
        existing.IsActive = constraint.IsActive;
        existing.UpdatedBy = constraint.UpdatedBy ?? PlatformConstants.SystemPrincipalId;

        await _constraintRepository.UpdateClassifierValueConstraintAsync(existing);
        // No 'constraint_edited' history op exists (spec §14.9); add/remove are audited, edits are not.
    }

    public async Task DeleteClassifierValueConstraintAsync(Guid id)
    {
        ClassifierValueConstraintDto existing = await GetExistingAsync(id);

        await _constraintRepository.DeleteClassifierValueConstraintAsync(id);

        await WriteHistoryAsync(existing.SourceClassifierId, "constraint_removed", PlatformConstants.SystemPrincipalId,
            $"Constraint {existing.ConstraintType} removed", JsonSerializer.Serialize(Snapshot(existing)));
    }

    // ----- helpers -----

    private async Task<ClassifierValueConstraintDto> GetExistingAsync(Guid id)
    {
        ClassifierValueConstraintDto? existing = await _constraintRepository.GetClassifierValueConstraintByIdAsync(id);
        if (existing == null)
            throw new NotFoundException($"Constraint with ID {id} was not found.");
        return existing;
    }

    private async Task EnsureValueBelongsToClassifierAsync(Guid valueId, Guid classifierId, string side)
    {
        ClassifierValueDto? value = await _valueRepository.GetClassifierValueByIdAsync(valueId);
        if (value == null)
            throw new BadRequestException($"The {side} value {valueId} was not found.");
        if (value.ClassifierId != classifierId)
            throw new BadRequestException($"The {side} value must belong to the {side} classifier ({(side == "source" ? "R8" : "R9")}).");
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

    private static void ValidateType(string constraintType)
    {
        if (string.IsNullOrWhiteSpace(constraintType) || !ConstraintTypes.Contains(constraintType))
            throw new BadRequestException($"constraint_type must be one of: {string.Join(", ", ConstraintTypes)}.");
    }

    private static object Snapshot(ClassifierValueConstraintDto c)
        => new
        {
            c.Id,
            c.SourceClassifierId,
            c.SourceValueId,
            c.TargetClassifierId,
            c.TargetValueId,
            c.ConstraintType,
            c.IsActive
        };
}
