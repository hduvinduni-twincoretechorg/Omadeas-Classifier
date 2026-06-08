using Moq;
using Omadeas.Classifier.Core.Constants;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Exceptions;
using Omadeas.Classifier.Core.Interfaces;
using Omadeas.Classifier.Core.Services;

namespace Omadeas.Classifier.Test.UnitTests.Core.Services;

public class ClassifierServiceTests
{
    private readonly Mock<IClassifierRepository> _repo = new();
    private readonly Mock<IClassifierHistoryRepository> _history = new();
    private readonly Mock<IClassifierValuePolicyRepository> _policy = new();
    private readonly ClassifierService _service;

    public ClassifierServiceTests()
    {
        _service = new ClassifierService(_repo.Object, _history.Object, _policy.Object);
    }

    private static ClassifierDto Cls(
        string code = "PRIORITY", string name = "Priority", string source = "platform", Guid? company = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Source = source,
            CompanyId = company ?? PlatformConstants.PlatformTenantId,
            Order = 10,
            IsActive = true
        };

    [Fact]
    public async Task GetById_throws_NotFound_when_missing()
    {
        _repo.Setup(r => r.GetClassifierByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ClassifierDto?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetClassifierByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Add_rejects_invalid_code()
    {
        await Assert.ThrowsAsync<BadRequestException>(() => _service.AddClassifierAsync(Cls(code: "bad code")));
    }

    [Fact]
    public async Task Add_rejects_platform_source_with_non_platform_company()
    {
        await Assert.ThrowsAsync<BadRequestException>(
            () => _service.AddClassifierAsync(Cls(source: "platform", company: Guid.NewGuid())));
    }

    [Fact]
    public async Task Add_rejects_tenant_source_with_platform_company()
    {
        await Assert.ThrowsAsync<BadRequestException>(
            () => _service.AddClassifierAsync(Cls(source: "tenant", company: PlatformConstants.PlatformTenantId)));
    }

    [Fact]
    public async Task Add_rejects_duplicate_code()
    {
        ClassifierDto dto = Cls();
        _repo.Setup(r => r.FindClassifiersByCodeOrNameAsync(dto.CompanyId, dto.Code, dto.Name))
             .ReturnsAsync(new[] { Cls() });

        await Assert.ThrowsAsync<BadRequestException>(() => _service.AddClassifierAsync(dto));
    }

    [Fact]
    public async Task Add_creates_classifier_default_policy_and_history()
    {
        ClassifierDto dto = Cls();
        ClassifierDto created = Cls();
        _repo.Setup(r => r.FindClassifiersByCodeOrNameAsync(dto.CompanyId, dto.Code, dto.Name))
             .ReturnsAsync(Array.Empty<ClassifierDto>());
        _repo.Setup(r => r.AddClassifierAsync(It.IsAny<ClassifierDto>())).ReturnsAsync(created);

        ClassifierDto result = await _service.AddClassifierAsync(dto);

        Assert.Equal(created.Id, result.Id);
        _policy.Verify(p => p.AddClassifierValuePolicyAsync(It.Is<ClassifierValuePolicyDto>(
            x => x.ClassifierId == created.Id
              && x.SelectionModeId == PlatformConstants.SelectionModeIds.SingleSelect
              && x.ComputationMode == PlatformConstants.ComputationMode.Manual)), Times.Once);
        _history.Verify(h => h.AddClassifierHistoryAsync(It.Is<ClassifierHistoryDto>(
            x => x.Operation == "created" && x.ClassifierId == created.Id)), Times.Once);
    }

    [Fact]
    public async Task Update_throws_NotFound_when_missing()
    {
        ClassifierDto dto = Cls();
        _repo.Setup(r => r.GetClassifierByIdAsync(dto.Id)).ReturnsAsync((ClassifierDto?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateClassifierAsync(dto));
    }

    [Fact]
    public async Task Update_is_noop_when_nothing_changed()
    {
        ClassifierDto existing = Cls();
        ClassifierDto dto = new()
        {
            Id = existing.Id,
            Name = existing.Name,
            Description = existing.Description,
            Order = existing.Order,
            AppliesToNodeTypes = existing.AppliesToNodeTypes
        };
        _repo.Setup(r => r.GetClassifierByIdAsync(existing.Id)).ReturnsAsync(existing);

        await _service.UpdateClassifierAsync(dto);

        _repo.Verify(r => r.UpdateClassifierAsync(It.IsAny<ClassifierDto>()), Times.Never);
        _history.Verify(h => h.AddClassifierHistoryAsync(It.IsAny<ClassifierHistoryDto>()), Times.Never);
    }

    [Fact]
    public async Task Update_rename_writes_renamed_history()
    {
        ClassifierDto existing = Cls();
        ClassifierDto dto = new() { Id = existing.Id, Name = "Priority 2", Order = existing.Order };
        _repo.Setup(r => r.GetClassifierByIdAsync(existing.Id)).ReturnsAsync(existing);
        _repo.Setup(r => r.FindClassifiersByCodeOrNameAsync(existing.CompanyId, existing.Code, "Priority 2"))
             .ReturnsAsync(Array.Empty<ClassifierDto>());

        await _service.UpdateClassifierAsync(dto);

        _repo.Verify(r => r.UpdateClassifierAsync(It.IsAny<ClassifierDto>()), Times.Once);
        _history.Verify(h => h.AddClassifierHistoryAsync(It.Is<ClassifierHistoryDto>(x => x.Operation == "renamed")), Times.Once);
    }

    [Fact]
    public async Task Retire_is_idempotent_when_already_inactive()
    {
        ClassifierDto existing = Cls();
        existing.IsActive = false;
        _repo.Setup(r => r.GetClassifierByIdAsync(existing.Id)).ReturnsAsync(existing);

        await _service.RetireClassifierAsync(existing.Id);

        _repo.Verify(r => r.SetClassifierActiveAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task Retire_active_classifier_sets_inactive_and_writes_history()
    {
        ClassifierDto existing = Cls();
        _repo.Setup(r => r.GetClassifierByIdAsync(existing.Id)).ReturnsAsync(existing);

        await _service.RetireClassifierAsync(existing.Id);

        _repo.Verify(r => r.SetClassifierActiveAsync(existing.Id, false, It.IsAny<Guid?>()), Times.Once);
        _history.Verify(h => h.AddClassifierHistoryAsync(It.Is<ClassifierHistoryDto>(x => x.Operation == "retired")), Times.Once);
    }
}
