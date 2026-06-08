using Moq;
using Omadeas.Classifier.Core.Constants;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Exceptions;
using Omadeas.Classifier.Core.Interfaces;
using Omadeas.Classifier.Core.Services;

namespace Omadeas.Classifier.Test.UnitTests.Core.Services;

public class ClassifierProfileServiceTests
{
    private readonly Mock<IClassifierProfileRepository> _repo = new();
    private readonly Mock<IClassifierProfileHistoryRepository> _history = new();
    private readonly ClassifierProfileService _service;

    public ClassifierProfileServiceTests()
    {
        _service = new ClassifierProfileService(_repo.Object, _history.Object);
    }

    private static ClassifierProfileDto Profile(
        string code = "RISK_GOV", string name = "Risk Governance", string source = "platform", Guid? company = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Source = source,
            CompanyId = company ?? PlatformConstants.PlatformTenantId,
            Order = 10,
            ScopeDefault = "subtree",
            IsActive = true
        };

    [Fact]
    public async Task GetById_throws_NotFound_when_missing()
    {
        _repo.Setup(r => r.GetClassifierProfileByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ClassifierProfileDto?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetClassifierProfileByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Add_rejects_invalid_code()
    {
        await Assert.ThrowsAsync<BadRequestException>(() => _service.AddClassifierProfileAsync(Profile(code: "bad code")));
    }

    [Fact]
    public async Task Add_rejects_platform_source_with_non_platform_company()
    {
        await Assert.ThrowsAsync<BadRequestException>(
            () => _service.AddClassifierProfileAsync(Profile(source: "platform", company: Guid.NewGuid())));
    }

    [Fact]
    public async Task Add_rejects_duplicate_name()
    {
        ClassifierProfileDto dto = Profile();
        _repo.Setup(r => r.FindClassifierProfilesByCodeOrNameAsync(dto.CompanyId, dto.Code, dto.Name))
             .ReturnsAsync(new[] { Profile() });

        await Assert.ThrowsAsync<BadRequestException>(() => _service.AddClassifierProfileAsync(dto));
    }

    [Fact]
    public async Task Add_succeeds_and_writes_created_history()
    {
        ClassifierProfileDto dto = Profile();
        ClassifierProfileDto created = Profile();
        _repo.Setup(r => r.FindClassifierProfilesByCodeOrNameAsync(dto.CompanyId, dto.Code, dto.Name))
             .ReturnsAsync(Array.Empty<ClassifierProfileDto>());
        _repo.Setup(r => r.AddClassifierProfileAsync(It.IsAny<ClassifierProfileDto>())).ReturnsAsync(created);

        await _service.AddClassifierProfileAsync(dto);

        _history.Verify(h => h.AddClassifierProfileHistoryAsync(It.Is<ClassifierProfileHistoryDto>(
            x => x.Operation == "created" && x.ProfileId == created.Id)), Times.Once);
    }

    [Fact]
    public async Task Update_scope_change_writes_scope_changed_history()
    {
        ClassifierProfileDto existing = Profile();
        ClassifierProfileDto dto = new()
        {
            Id = existing.Id,
            Name = existing.Name,
            Order = existing.Order,
            Description = existing.Description,
            ScopeDefault = "self"
        };
        _repo.Setup(r => r.GetClassifierProfileByIdAsync(existing.Id)).ReturnsAsync(existing);

        await _service.UpdateClassifierProfileAsync(dto);

        _history.Verify(h => h.AddClassifierProfileHistoryAsync(It.Is<ClassifierProfileHistoryDto>(x => x.Operation == "scope_changed")), Times.Once);
    }

    [Fact]
    public async Task Update_is_noop_when_nothing_changed()
    {
        ClassifierProfileDto existing = Profile();
        ClassifierProfileDto dto = new()
        {
            Id = existing.Id,
            Name = existing.Name,
            Order = existing.Order,
            Description = existing.Description,
            ScopeDefault = existing.ScopeDefault
        };
        _repo.Setup(r => r.GetClassifierProfileByIdAsync(existing.Id)).ReturnsAsync(existing);

        await _service.UpdateClassifierProfileAsync(dto);

        _repo.Verify(r => r.UpdateClassifierProfileAsync(It.IsAny<ClassifierProfileDto>()), Times.Never);
        _history.Verify(h => h.AddClassifierProfileHistoryAsync(It.IsAny<ClassifierProfileHistoryDto>()), Times.Never);
    }

    [Fact]
    public async Task Retire_active_profile_writes_retired_history()
    {
        ClassifierProfileDto existing = Profile();
        _repo.Setup(r => r.GetClassifierProfileByIdAsync(existing.Id)).ReturnsAsync(existing);

        await _service.RetireClassifierProfileAsync(existing.Id);

        _repo.Verify(r => r.SetClassifierProfileActiveAsync(existing.Id, false, It.IsAny<Guid?>()), Times.Once);
        _history.Verify(h => h.AddClassifierProfileHistoryAsync(It.Is<ClassifierProfileHistoryDto>(x => x.Operation == "retired")), Times.Once);
    }
}
