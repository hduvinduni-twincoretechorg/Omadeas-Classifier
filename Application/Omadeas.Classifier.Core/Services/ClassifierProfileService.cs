using System.Text.RegularExpressions;
using Omadeas.Classifier.Core.Constants;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Exceptions;
using Omadeas.Classifier.Core.Interfaces;

namespace Omadeas.Classifier.Core.Services;

public class ClassifierProfileService : IClassifierProfileService
{
    private static readonly Regex CodePattern = new("^[A-Z][A-Z0-9_]+$", RegexOptions.Compiled);
    private static readonly string[] ScopeDefaults = { "self", "subtree" };

    private readonly IClassifierProfileRepository _profileRepository;

    public ClassifierProfileService(IClassifierProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<IEnumerable<ClassifierProfileDto>> GetAllClassifierProfilesAsync(Guid? companyId, string? source, bool? isActive)
    {
        return await _profileRepository.GetAllClassifierProfilesAsync(companyId, source, isActive);
    }

    public async Task<ClassifierProfileDto> GetClassifierProfileByIdAsync(Guid id)
    {
        return await GetExistingAsync(id);
    }

    public async Task<ClassifierProfileDto> AddClassifierProfileAsync(ClassifierProfileDto profile)
    {
        ValidateCommonFields(profile);
        ValidateSourceCoherence(profile);

        if (profile.Order <= 0)
            profile.Order = 100;
        if (string.IsNullOrWhiteSpace(profile.ScopeDefault))
            profile.ScopeDefault = "subtree";

        IEnumerable<ClassifierProfileDto> clashes = await _profileRepository.FindClassifierProfilesByCodeOrNameAsync(
            profile.CompanyId, profile.Code, profile.Name);
        if (clashes.Any(p => string.Equals(p.Code, profile.Code, StringComparison.OrdinalIgnoreCase)))
            throw new BadRequestException($"A profile with code '{profile.Code}' already exists for this company.");
        if (clashes.Any(p => string.Equals(p.Name, profile.Name, StringComparison.OrdinalIgnoreCase)))
            throw new BadRequestException($"A profile with name '{profile.Name}' already exists for this company.");

        profile.CreatedBy ??= PlatformConstants.SystemPrincipalId;

        return await _profileRepository.AddClassifierProfileAsync(profile);
    }

    public async Task UpdateClassifierProfileAsync(ClassifierProfileDto profile)
    {
        ClassifierProfileDto existing = await GetExistingAsync(profile.Id);

        if (string.IsNullOrWhiteSpace(profile.Name) || profile.Name.Length > 100)
            throw new BadRequestException("Profile name is required and must be at most 100 characters.");
        if (profile.Description is { Length: > 500 })
            throw new BadRequestException("Profile description must be at most 500 characters.");
        ValidateScopeDefault(profile.ScopeDefault);
        if (profile.Order <= 0)
            profile.Order = 100;

        // code, source and company_id are immutable.
        profile.CompanyId = existing.CompanyId;
        profile.Source = existing.Source;
        profile.Code = existing.Code;

        if (!string.Equals(existing.Name, profile.Name, StringComparison.Ordinal))
        {
            IEnumerable<ClassifierProfileDto> clashes = await _profileRepository.FindClassifierProfilesByCodeOrNameAsync(
                existing.CompanyId, existing.Code, profile.Name);
            if (clashes.Any(p => p.Id != existing.Id &&
                                 string.Equals(p.Name, profile.Name, StringComparison.OrdinalIgnoreCase)))
                throw new BadRequestException($"A profile with name '{profile.Name}' already exists for this company.");
        }

        profile.UpdatedBy ??= PlatformConstants.SystemPrincipalId;
        await _profileRepository.UpdateClassifierProfileAsync(profile);
    }

    public async Task RetireClassifierProfileAsync(Guid id)
    {
        ClassifierProfileDto existing = await GetExistingAsync(id);
        if (!existing.IsActive)
            return;
        await _profileRepository.SetClassifierProfileActiveAsync(id, false, PlatformConstants.SystemPrincipalId);
    }

    public async Task ReactivateClassifierProfileAsync(Guid id)
    {
        ClassifierProfileDto existing = await GetExistingAsync(id);
        if (existing.IsActive)
            return;
        await _profileRepository.SetClassifierProfileActiveAsync(id, true, PlatformConstants.SystemPrincipalId);
    }

    // ----- helpers -----

    private async Task<ClassifierProfileDto> GetExistingAsync(Guid id)
    {
        ClassifierProfileDto? existing = await _profileRepository.GetClassifierProfileByIdAsync(id);
        if (existing == null)
            throw new NotFoundException($"Profile with ID {id} was not found.");
        return existing;
    }

    private static void ValidateCommonFields(ClassifierProfileDto profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Code) || profile.Code.Length > 50 || !CodePattern.IsMatch(profile.Code))
            throw new BadRequestException("Profile code is required, must be UPPER_SNAKE_CASE (^[A-Z][A-Z0-9_]+$) and at most 50 characters.");
        if (string.IsNullOrWhiteSpace(profile.Name) || profile.Name.Length > 100)
            throw new BadRequestException("Profile name is required and must be at most 100 characters.");
        if (profile.Description is { Length: > 500 })
            throw new BadRequestException("Profile description must be at most 500 characters.");
        ValidateScopeDefault(profile.ScopeDefault);
    }

    private static void ValidateScopeDefault(string scopeDefault)
    {
        if (!string.IsNullOrWhiteSpace(scopeDefault) && !ScopeDefaults.Contains(scopeDefault))
            throw new BadRequestException("scope_default must be 'self' or 'subtree'.");
    }

    private static void ValidateSourceCoherence(ClassifierProfileDto profile)
    {
        switch (profile.Source)
        {
            case PlatformConstants.Source.Platform:
                if (profile.CompanyId != PlatformConstants.PlatformTenantId)
                    throw new BadRequestException("Platform profiles must use the reserved PLATFORM_TENANT_ID as company_id.");
                break;
            case PlatformConstants.Source.Tenant:
                if (profile.CompanyId == Guid.Empty || profile.CompanyId == PlatformConstants.PlatformTenantId)
                    throw new BadRequestException("Tenant profiles must use a real tenant company_id (not empty, not the platform id).");
                break;
            default:
                throw new BadRequestException("Profile source must be 'platform' or 'tenant'.");
        }
    }
}
