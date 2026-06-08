using Moq;
using Omadeas.Classifier.Core.Constants;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Exceptions;
using Omadeas.Classifier.Core.Interfaces;
using Omadeas.Classifier.Core.Services;

namespace Omadeas.Classifier.Test.UnitTests.Core.Services;

public class ClassifierValuePolicyServiceTests
{
    private readonly Mock<IClassifierValuePolicyRepository> _policies = new();
    private readonly Mock<IClassifierRepository> _classifiers = new();
    private readonly Mock<IClassifierSelectionModeRepository> _modes = new();
    private readonly Mock<IClassifierValueRepository> _values = new();
    private readonly Mock<IClassifierHistoryRepository> _history = new();
    private readonly ClassifierValuePolicyService _service;

    private readonly Guid _classifierId = Guid.NewGuid();
    private readonly Guid _modeId = PlatformConstants.SelectionModeIds.SingleSelect;

    public ClassifierValuePolicyServiceTests()
    {
        _service = new ClassifierValuePolicyService(
            _policies.Object, _classifiers.Object, _modes.Object, _values.Object, _history.Object);
    }

    private void ClassifierExists()
        => _classifiers.Setup(r => r.GetClassifierByIdAsync(_classifierId))
                       .ReturnsAsync(new ClassifierDto { Id = _classifierId, Code = "PRIORITY", Name = "Priority" });

    private void ModeExists()
        => _modes.Setup(r => r.GetAllClassifierSelectionModesAsync(null))
                 .ReturnsAsync(new[] { new ClassifierSelectionModeDto { Id = _modeId, Code = "SINGLE_SELECT" } });

    private ClassifierValuePolicyDto Policy()
        => new() { ClassifierId = _classifierId, SelectionModeId = _modeId, ComputationMode = "manual" };

    [Fact]
    public async Task Get_throws_NotFound_when_classifier_missing()
    {
        _classifiers.Setup(r => r.GetClassifierByIdAsync(_classifierId)).ReturnsAsync((ClassifierDto?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetClassifierValuePolicyAsync(_classifierId));
    }

    [Fact]
    public async Task Get_throws_NotFound_when_policy_missing()
    {
        ClassifierExists();
        _policies.Setup(r => r.GetClassifierValuePolicyByClassifierAsync(_classifierId)).ReturnsAsync((ClassifierValuePolicyDto?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetClassifierValuePolicyAsync(_classifierId));
    }

    [Fact]
    public async Task Create_rejects_when_policy_already_exists()
    {
        ClassifierExists();
        _policies.Setup(r => r.GetClassifierValuePolicyByClassifierAsync(_classifierId)).ReturnsAsync(Policy());
        await Assert.ThrowsAsync<BadRequestException>(() => _service.CreateClassifierValuePolicyAsync(_classifierId, Policy()));
    }

    [Fact]
    public async Task Create_rejects_non_manual_computation_mode()
    {
        ClassifierExists();
        _policies.Setup(r => r.GetClassifierValuePolicyByClassifierAsync(_classifierId)).ReturnsAsync((ClassifierValuePolicyDto?)null);
        ClassifierValuePolicyDto dto = Policy();
        dto.ComputationMode = "computed";
        await Assert.ThrowsAsync<BadRequestException>(() => _service.CreateClassifierValuePolicyAsync(_classifierId, dto));
    }

    [Fact]
    public async Task Create_rejects_calc_engine_reference_in_v2()
    {
        ClassifierExists();
        _policies.Setup(r => r.GetClassifierValuePolicyByClassifierAsync(_classifierId)).ReturnsAsync((ClassifierValuePolicyDto?)null);
        ClassifierValuePolicyDto dto = Policy();
        dto.DefaultCalculationId = Guid.NewGuid();
        await Assert.ThrowsAsync<BadRequestException>(() => _service.CreateClassifierValuePolicyAsync(_classifierId, dto));
    }

    [Fact]
    public async Task Create_rejects_unknown_selection_mode()
    {
        ClassifierExists();
        _policies.Setup(r => r.GetClassifierValuePolicyByClassifierAsync(_classifierId)).ReturnsAsync((ClassifierValuePolicyDto?)null);
        _modes.Setup(r => r.GetAllClassifierSelectionModesAsync(null)).ReturnsAsync(Array.Empty<ClassifierSelectionModeDto>());
        await Assert.ThrowsAsync<BadRequestException>(() => _service.CreateClassifierValuePolicyAsync(_classifierId, Policy()));
    }

    [Fact]
    public async Task Create_rejects_default_value_from_other_classifier()
    {
        ClassifierExists();
        ModeExists();
        _policies.Setup(r => r.GetClassifierValuePolicyByClassifierAsync(_classifierId)).ReturnsAsync((ClassifierValuePolicyDto?)null);
        ClassifierValuePolicyDto dto = Policy();
        dto.DefaultValueId = Guid.NewGuid();
        _values.Setup(r => r.GetClassifierValueByIdAsync(dto.DefaultValueId.Value))
               .ReturnsAsync(new ClassifierValueDto { Id = dto.DefaultValueId.Value, ClassifierId = Guid.NewGuid() });

        await Assert.ThrowsAsync<BadRequestException>(() => _service.CreateClassifierValuePolicyAsync(_classifierId, dto));
    }

    [Fact]
    public async Task Create_succeeds_and_writes_policy_edit_history()
    {
        ClassifierExists();
        ModeExists();
        _policies.Setup(r => r.GetClassifierValuePolicyByClassifierAsync(_classifierId)).ReturnsAsync((ClassifierValuePolicyDto?)null);
        _policies.Setup(r => r.AddClassifierValuePolicyAsync(It.IsAny<ClassifierValuePolicyDto>())).ReturnsAsync(Policy());

        await _service.CreateClassifierValuePolicyAsync(_classifierId, Policy());

        _history.Verify(h => h.AddClassifierHistoryAsync(It.Is<ClassifierHistoryDto>(x => x.Operation == "policy_edit")), Times.Once);
    }

    [Fact]
    public async Task Update_throws_NotFound_when_policy_missing()
    {
        ClassifierExists();
        _policies.Setup(r => r.GetClassifierValuePolicyByClassifierAsync(_classifierId)).ReturnsAsync((ClassifierValuePolicyDto?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateClassifierValuePolicyAsync(_classifierId, Policy()));
    }
}
