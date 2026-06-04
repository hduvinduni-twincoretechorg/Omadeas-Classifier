using System.Text.Json;
using System.Text.RegularExpressions;
using Omadeas.Classifier.Core.Constants;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Exceptions;
using Omadeas.Classifier.Core.Interfaces;

namespace Omadeas.Classifier.Core.Services;

public class ClassifierService : IClassifierService
{
    private static readonly Regex CodePattern = new("^[A-Z][A-Z0-9_]+$", RegexOptions.Compiled);

    private readonly IClassifierRepository _classifierRepository;
    private readonly IClassifierHistoryRepository _historyRepository;
    private readonly IClassifierValuePolicyRepository _policyRepository;

    public ClassifierService(
        IClassifierRepository classifierRepository,
        IClassifierHistoryRepository historyRepository,
        IClassifierValuePolicyRepository policyRepository)
    {
        _classifierRepository = classifierRepository;
        _historyRepository = historyRepository;
        _policyRepository = policyRepository;
    }

    public async Task<IEnumerable<ClassifierDto>> GetAllClassifiersAsync(Guid? companyId, string? source, bool? isActive)
    {
        return await _classifierRepository.GetAllClassifiersAsync(companyId, source, isActive);
    }

    public async Task<ClassifierDto> GetClassifierByIdAsync(Guid id)
    {
        return await GetExistingAsync(id);
    }

    public async Task<ClassifierDto> AddClassifierAsync(ClassifierDto classifier)
    {
        ValidateCommonFields(classifier);
        ValidateSourceCoherence(classifier);

        if (classifier.Order <= 0)
            classifier.Order = 100;

        IEnumerable<ClassifierDto> clashes = await _classifierRepository.FindClassifiersByCodeOrNameAsync(
            classifier.CompanyId, classifier.Code, classifier.Name);
        if (clashes.Any(c => string.Equals(c.Code, classifier.Code, StringComparison.OrdinalIgnoreCase)))
            throw new BadRequestException($"A classifier with code '{classifier.Code}' already exists for this company.");
        if (clashes.Any(c => string.Equals(c.Name, classifier.Name, StringComparison.OrdinalIgnoreCase)))
            throw new BadRequestException($"A classifier with name '{classifier.Name}' already exists for this company.");

        Guid principalId = classifier.CreatedBy ?? PlatformConstants.SystemPrincipalId;
        classifier.CreatedBy = principalId;

        ClassifierDto created = await _classifierRepository.AddClassifierAsync(classifier);

        // Spec R2: a policy is created together with the classifier.
        // Default: SINGLE_SELECT, optional, manual. Admins refine it via the policy endpoint.
        await _policyRepository.AddClassifierValuePolicyAsync(new ClassifierValuePolicyDto
        {
            ClassifierId = created.Id,
            SelectionModeId = PlatformConstants.SelectionModeIds.SingleSelect,
            IsRequired = false,
            AllowCustomValues = false,
            RequiresReasonOnChange = false,
            ComputationMode = PlatformConstants.ComputationMode.Manual,
            CreatedBy = principalId
        });

        await WriteHistoryAsync(created.Id, "created", principalId,
            $"Classifier '{created.Name}' created", JsonSerializer.Serialize(Snapshot(created)));

        return created;
    }

    public async Task UpdateClassifierAsync(ClassifierDto classifier)
    {
        ClassifierDto existing = await GetExistingAsync(classifier.Id);

        if (string.IsNullOrWhiteSpace(classifier.Name) || classifier.Name.Length > 100)
            throw new BadRequestException("Classifier name is required and must be at most 100 characters.");
        if (classifier.Description is { Length: > 500 })
            throw new BadRequestException("Classifier description must be at most 500 characters.");
        if (classifier.Order <= 0)
            classifier.Order = 100;

        // code, source and company_id are immutable — preserve the persisted values.
        classifier.CompanyId = existing.CompanyId;
        classifier.Source = existing.Source;
        classifier.Code = existing.Code;

        bool nameChanged = !string.Equals(existing.Name, classifier.Name, StringComparison.Ordinal);
        bool orderChanged = existing.Order != classifier.Order;
        bool descriptionChanged = !string.Equals(existing.Description, classifier.Description, StringComparison.Ordinal);
        bool nodeTypesChanged = NodeTypesChanged(existing.AppliesToNodeTypes, classifier.AppliesToNodeTypes);

        if (!nameChanged && !orderChanged && !descriptionChanged && !nodeTypesChanged)
            return; // nothing to do — no write, no history

        if (nameChanged)
        {
            IEnumerable<ClassifierDto> clashes = await _classifierRepository.FindClassifiersByCodeOrNameAsync(
                existing.CompanyId, existing.Code, classifier.Name);
            if (clashes.Any(c => c.Id != existing.Id &&
                                 string.Equals(c.Name, classifier.Name, StringComparison.OrdinalIgnoreCase)))
                throw new BadRequestException($"A classifier with name '{classifier.Name}' already exists for this company.");
        }

        Guid principalId = classifier.UpdatedBy ?? PlatformConstants.SystemPrincipalId;
        classifier.UpdatedBy = principalId;

        await _classifierRepository.UpdateClassifierAsync(classifier);

        // The definition-history enum has no generic "edited" op (see plan §9); use the most
        // specific available operation by precedence and let the summary carry the detail.
        string operation = nameChanged ? "renamed" : orderChanged ? "reordered" : "renamed";
        string summary = BuildUpdateSummary(nameChanged, orderChanged, descriptionChanged, nodeTypesChanged, classifier.Name);
        string payload = JsonSerializer.Serialize(new { before = Snapshot(existing), after = Snapshot(classifier) });

        await WriteHistoryAsync(existing.Id, operation, principalId, summary, payload);
    }

    public async Task RetireClassifierAsync(Guid id)
    {
        ClassifierDto existing = await GetExistingAsync(id);
        if (!existing.IsActive)
            return; // already retired — idempotent no-op

        await _classifierRepository.SetClassifierActiveAsync(id, false, PlatformConstants.SystemPrincipalId);
        await WriteHistoryAsync(id, "retired", PlatformConstants.SystemPrincipalId,
            $"Classifier '{existing.Name}' retired", JsonSerializer.Serialize(new { isActive = false }));
    }

    public async Task ReactivateClassifierAsync(Guid id)
    {
        ClassifierDto existing = await GetExistingAsync(id);
        if (existing.IsActive)
            return; // already active — idempotent no-op

        await _classifierRepository.SetClassifierActiveAsync(id, true, PlatformConstants.SystemPrincipalId);
        await WriteHistoryAsync(id, "activated", PlatformConstants.SystemPrincipalId,
            $"Classifier '{existing.Name}' reactivated", JsonSerializer.Serialize(new { isActive = true }));
    }

    public async Task<IEnumerable<ClassifierHistoryDto>> GetClassifierHistoryAsync(Guid id)
    {
        await GetExistingAsync(id); // 404 if the classifier doesn't exist
        return await _historyRepository.GetClassifierHistoryAsync(id);
    }

    // ----- helpers -----

    private async Task<ClassifierDto> GetExistingAsync(Guid id)
    {
        ClassifierDto? existing = await _classifierRepository.GetClassifierByIdAsync(id);
        if (existing == null)
            throw new NotFoundException($"Classifier with ID {id} was not found.");
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

    private static void ValidateCommonFields(ClassifierDto classifier)
    {
        if (string.IsNullOrWhiteSpace(classifier.Code) || classifier.Code.Length > 50 || !CodePattern.IsMatch(classifier.Code))
            throw new BadRequestException("Classifier code is required, must be UPPER_SNAKE_CASE (^[A-Z][A-Z0-9_]+$) and at most 50 characters.");
        if (string.IsNullOrWhiteSpace(classifier.Name) || classifier.Name.Length > 100)
            throw new BadRequestException("Classifier name is required and must be at most 100 characters.");
        if (classifier.Description is { Length: > 500 })
            throw new BadRequestException("Classifier description must be at most 500 characters.");
    }

    private static void ValidateSourceCoherence(ClassifierDto classifier)
    {
        switch (classifier.Source)
        {
            case PlatformConstants.Source.Platform:
                if (classifier.CompanyId != PlatformConstants.PlatformTenantId)
                    throw new BadRequestException("Platform classifiers must use the reserved PLATFORM_TENANT_ID as company_id.");
                break;
            case PlatformConstants.Source.Tenant:
                if (classifier.CompanyId == Guid.Empty || classifier.CompanyId == PlatformConstants.PlatformTenantId)
                    throw new BadRequestException("Tenant classifiers must use a real tenant company_id (not empty, not the platform id).");
                break;
            default:
                throw new BadRequestException("Classifier source must be 'platform' or 'tenant'.");
        }
    }

    private static bool NodeTypesChanged(List<string>? a, List<string>? b)
    {
        List<string> left = a ?? new List<string>();
        List<string> right = b ?? new List<string>();
        return !left.SequenceEqual(right);
    }

    private static string BuildUpdateSummary(bool name, bool order, bool description, bool nodeTypes, string newName)
    {
        List<string> parts = new();
        if (name) parts.Add("name");
        if (order) parts.Add("order");
        if (description) parts.Add("description");
        if (nodeTypes) parts.Add("applies_to_node_types");
        return $"Classifier '{newName}' updated ({string.Join(", ", parts)})";
    }

    private static object Snapshot(ClassifierDto c)
        => new
        {
            c.Id,
            c.CompanyId,
            c.Source,
            c.Code,
            c.Name,
            c.Description,
            c.Order,
            c.AppliesToNodeTypes,
            c.IsActive
        };
}
