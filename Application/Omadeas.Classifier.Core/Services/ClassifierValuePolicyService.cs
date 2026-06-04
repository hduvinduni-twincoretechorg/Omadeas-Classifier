using System.Text.Json;
using Omadeas.Classifier.Core.Constants;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Exceptions;
using Omadeas.Classifier.Core.Interfaces;

namespace Omadeas.Classifier.Core.Services;

public class ClassifierValuePolicyService : IClassifierValuePolicyService
{
    private readonly IClassifierValuePolicyRepository _policyRepository;
    private readonly IClassifierRepository _classifierRepository;
    private readonly IClassifierSelectionModeRepository _selectionModeRepository;
    private readonly IClassifierValueRepository _valueRepository;
    private readonly IClassifierHistoryRepository _historyRepository;

    public ClassifierValuePolicyService(
        IClassifierValuePolicyRepository policyRepository,
        IClassifierRepository classifierRepository,
        IClassifierSelectionModeRepository selectionModeRepository,
        IClassifierValueRepository valueRepository,
        IClassifierHistoryRepository historyRepository)
    {
        _policyRepository = policyRepository;
        _classifierRepository = classifierRepository;
        _selectionModeRepository = selectionModeRepository;
        _valueRepository = valueRepository;
        _historyRepository = historyRepository;
    }

    public async Task<ClassifierValuePolicyDto> GetClassifierValuePolicyAsync(Guid classifierId)
    {
        await EnsureClassifierExistsAsync(classifierId);
        ClassifierValuePolicyDto? policy = await _policyRepository.GetClassifierValuePolicyByClassifierAsync(classifierId);
        if (policy == null)
            throw new NotFoundException($"Classifier {classifierId} has no policy.");
        return policy;
    }

    public async Task<ClassifierValuePolicyDto> CreateClassifierValuePolicyAsync(Guid classifierId, ClassifierValuePolicyDto policy)
    {
        await EnsureClassifierExistsAsync(classifierId);

        ClassifierValuePolicyDto? existing = await _policyRepository.GetClassifierValuePolicyByClassifierAsync(classifierId);
        if (existing != null)
            throw new BadRequestException($"Classifier {classifierId} already has a policy; update it instead.");

        policy.ClassifierId = classifierId;
        NormaliseAndValidate(policy);

        await ValidateReferencesAsync(classifierId, policy);

        Guid principalId = policy.CreatedBy ?? PlatformConstants.SystemPrincipalId;
        policy.CreatedBy = principalId;

        ClassifierValuePolicyDto created = await _policyRepository.AddClassifierValuePolicyAsync(policy);

        await WriteHistoryAsync(classifierId, principalId, "Policy created", JsonSerializer.Serialize(Snapshot(created)));

        return created;
    }

    public async Task UpdateClassifierValuePolicyAsync(Guid classifierId, ClassifierValuePolicyDto policy)
    {
        await EnsureClassifierExistsAsync(classifierId);

        ClassifierValuePolicyDto? existing = await _policyRepository.GetClassifierValuePolicyByClassifierAsync(classifierId);
        if (existing == null)
            throw new NotFoundException($"Classifier {classifierId} has no policy to update.");

        policy.ClassifierId = classifierId;
        NormaliseAndValidate(policy);
        await ValidateReferencesAsync(classifierId, policy);

        Guid principalId = policy.UpdatedBy ?? PlatformConstants.SystemPrincipalId;
        policy.UpdatedBy = principalId;

        await _policyRepository.UpdateClassifierValuePolicyAsync(policy);

        string payload = JsonSerializer.Serialize(new { before = Snapshot(existing), after = Snapshot(policy) });
        await WriteHistoryAsync(classifierId, principalId, "Policy edited", payload);
    }

    // ----- helpers -----

    private async Task EnsureClassifierExistsAsync(Guid classifierId)
    {
        ClassifierDto? classifier = await _classifierRepository.GetClassifierByIdAsync(classifierId);
        if (classifier == null)
            throw new NotFoundException($"Classifier with ID {classifierId} was not found.");
    }

    private static void NormaliseAndValidate(ClassifierValuePolicyDto policy)
    {
        if (policy.SelectionModeId == Guid.Empty)
            throw new BadRequestException("A selection mode is required.");

        if (string.IsNullOrWhiteSpace(policy.ComputationMode))
            policy.ComputationMode = PlatformConstants.ComputationMode.Manual;
        if (policy.ComputationMode != PlatformConstants.ComputationMode.Manual)
            throw new BadRequestException("v2 supports only computation_mode='manual'.");

        if (policy.MinSelected is < 0)
            throw new BadRequestException("min_selected cannot be negative.");
        if (policy.MaxSelected is < 1)
            throw new BadRequestException("max_selected must be at least 1.");
        if (policy.MinSelected.HasValue && policy.MaxSelected.HasValue && policy.MinSelected > policy.MaxSelected)
            throw new BadRequestException("min_selected cannot exceed max_selected.");

        // Calc-Engine reference is v3; must be null in v2.
        if (policy.DefaultCalculationId.HasValue)
            throw new BadRequestException("default_calculation_id is reserved for v3 and must be null.");
    }

    private async Task ValidateReferencesAsync(Guid classifierId, ClassifierValuePolicyDto policy)
    {
        IEnumerable<ClassifierSelectionModeDto> modes = await _selectionModeRepository.GetAllClassifierSelectionModesAsync(null);
        if (modes.All(m => m.Id != policy.SelectionModeId))
            throw new BadRequestException($"Selection mode {policy.SelectionModeId} does not exist.");

        if (policy.DefaultValueId.HasValue)
        {
            ClassifierValueDto? defaultValue = await _valueRepository.GetClassifierValueByIdAsync(policy.DefaultValueId.Value);
            if (defaultValue == null || defaultValue.ClassifierId != classifierId)
                throw new BadRequestException("default_value_id must reference a value of this classifier (R5).");
        }
    }

    private Task WriteHistoryAsync(Guid classifierId, Guid occurredBy, string summary, string? payload)
        => _historyRepository.AddClassifierHistoryAsync(new ClassifierHistoryDto
        {
            ClassifierId = classifierId,
            Operation = "policy_edit",
            OccurredBy = occurredBy,
            OccurredAt = DateTime.UtcNow,
            Summary = summary,
            Payload = payload
        });

    private static object Snapshot(ClassifierValuePolicyDto p)
        => new
        {
            p.Id,
            p.ClassifierId,
            p.SelectionModeId,
            p.MinSelected,
            p.MaxSelected,
            p.IsRequired,
            p.DefaultValueId,
            p.AllowCustomValues,
            p.RequiresReasonOnChange,
            p.ComputationMode,
            p.IsActive
        };
}
