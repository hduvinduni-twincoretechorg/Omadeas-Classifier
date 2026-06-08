using Moq;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Exceptions;
using Omadeas.Classifier.Core.Interfaces;
using Omadeas.Classifier.Core.Services;

namespace Omadeas.Classifier.Test.UnitTests.Core.Services;

public class ClassifierValueConstraintServiceTests
{
    private readonly Mock<IClassifierValueConstraintRepository> _constraints = new();
    private readonly Mock<IClassifierValueRepository> _values = new();
    private readonly Mock<IClassifierHistoryRepository> _history = new();
    private readonly ClassifierValueConstraintService _service;

    private readonly Guid _classifierId = Guid.NewGuid();
    private readonly Guid _sourceValueId = Guid.NewGuid();
    private readonly Guid _targetValueId = Guid.NewGuid();

    public ClassifierValueConstraintServiceTests()
    {
        _service = new ClassifierValueConstraintService(_constraints.Object, _values.Object, _history.Object);
    }

    private ClassifierValueConstraintDto Constraint(string type = "REQUIRES")
        => new()
        {
            Id = Guid.NewGuid(),
            SourceClassifierId = _classifierId,
            SourceValueId = _sourceValueId,
            TargetClassifierId = _classifierId,
            TargetValueId = _targetValueId,
            ConstraintType = type,
            IsActive = true
        };

    private void ValuesBelongToClassifier()
    {
        _values.Setup(r => r.GetClassifierValueByIdAsync(_sourceValueId))
               .ReturnsAsync(new ClassifierValueDto { Id = _sourceValueId, ClassifierId = _classifierId });
        _values.Setup(r => r.GetClassifierValueByIdAsync(_targetValueId))
               .ReturnsAsync(new ClassifierValueDto { Id = _targetValueId, ClassifierId = _classifierId });
    }

    [Fact]
    public async Task GetById_throws_NotFound_when_missing()
    {
        _constraints.Setup(r => r.GetClassifierValueConstraintByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ClassifierValueConstraintDto?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetClassifierValueConstraintByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Add_rejects_invalid_type()
    {
        await Assert.ThrowsAsync<BadRequestException>(() => _service.AddClassifierValueConstraintAsync(Constraint("BOGUS")));
    }

    [Fact]
    public async Task Add_rejects_identical_source_and_target_value()
    {
        ClassifierValueConstraintDto dto = Constraint();
        dto.TargetValueId = dto.SourceValueId;
        await Assert.ThrowsAsync<BadRequestException>(() => _service.AddClassifierValueConstraintAsync(dto));
    }

    [Fact]
    public async Task Add_rejects_source_value_not_in_source_classifier()
    {
        ClassifierValueConstraintDto dto = Constraint();
        _values.Setup(r => r.GetClassifierValueByIdAsync(_sourceValueId))
               .ReturnsAsync(new ClassifierValueDto { Id = _sourceValueId, ClassifierId = Guid.NewGuid() });

        await Assert.ThrowsAsync<BadRequestException>(() => _service.AddClassifierValueConstraintAsync(dto));
    }

    [Fact]
    public async Task Add_succeeds_and_writes_constraint_added_history()
    {
        ClassifierValueConstraintDto dto = Constraint();
        ClassifierValueConstraintDto created = Constraint();
        ValuesBelongToClassifier();
        _constraints.Setup(r => r.AddClassifierValueConstraintAsync(It.IsAny<ClassifierValueConstraintDto>())).ReturnsAsync(created);

        await _service.AddClassifierValueConstraintAsync(dto);

        _history.Verify(h => h.AddClassifierHistoryAsync(It.Is<ClassifierHistoryDto>(
            x => x.Operation == "constraint_added" && x.ClassifierId == created.SourceClassifierId)), Times.Once);
    }

    [Fact]
    public async Task Delete_writes_constraint_removed_history()
    {
        ClassifierValueConstraintDto existing = Constraint();
        _constraints.Setup(r => r.GetClassifierValueConstraintByIdAsync(existing.Id)).ReturnsAsync(existing);

        await _service.DeleteClassifierValueConstraintAsync(existing.Id);

        _constraints.Verify(r => r.DeleteClassifierValueConstraintAsync(existing.Id), Times.Once);
        _history.Verify(h => h.AddClassifierHistoryAsync(It.Is<ClassifierHistoryDto>(x => x.Operation == "constraint_removed")), Times.Once);
    }

    [Fact]
    public async Task Delete_throws_NotFound_when_missing()
    {
        _constraints.Setup(r => r.GetClassifierValueConstraintByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ClassifierValueConstraintDto?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteClassifierValueConstraintAsync(Guid.NewGuid()));
    }
}
