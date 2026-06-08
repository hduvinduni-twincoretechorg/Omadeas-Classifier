using Moq;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Exceptions;
using Omadeas.Classifier.Core.Interfaces;
using Omadeas.Classifier.Core.Services;

namespace Omadeas.Classifier.Test.UnitTests.Core.Services;

public class ClassifierValueServiceTests
{
    private readonly Mock<IClassifierValueRepository> _values = new();
    private readonly Mock<IClassifierRepository> _classifiers = new();
    private readonly Mock<IClassifierHistoryRepository> _history = new();
    private readonly ClassifierValueService _service;

    private readonly Guid _classifierId = Guid.NewGuid();

    public ClassifierValueServiceTests()
    {
        _service = new ClassifierValueService(_values.Object, _classifiers.Object, _history.Object);
    }

    private void ClassifierExists()
        => _classifiers.Setup(r => r.GetClassifierByIdAsync(_classifierId))
                       .ReturnsAsync(new ClassifierDto { Id = _classifierId, Code = "PRIORITY", Name = "Priority" });

    private ClassifierValueDto Value(string code = "HIGH", string name = "High", string? colour = "#ef4444")
        => new() { Id = Guid.NewGuid(), ClassifierId = _classifierId, Code = code, Name = name, Colour = colour, Order = 10, IsActive = true };

    [Fact]
    public async Task GetValues_throws_NotFound_when_classifier_missing()
    {
        _classifiers.Setup(r => r.GetClassifierByIdAsync(_classifierId)).ReturnsAsync((ClassifierDto?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetClassifierValuesAsync(_classifierId, null));
    }

    [Fact]
    public async Task GetById_throws_NotFound_when_value_belongs_to_other_classifier()
    {
        ClassifierValueDto other = Value();
        other.ClassifierId = Guid.NewGuid();
        _values.Setup(r => r.GetClassifierValueByIdAsync(other.Id)).ReturnsAsync(other);
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetClassifierValueByIdAsync(_classifierId, other.Id));
    }

    [Fact]
    public async Task Add_rejects_invalid_colour()
    {
        ClassifierExists();
        await Assert.ThrowsAsync<BadRequestException>(() => _service.AddClassifierValueAsync(Value(colour: "red")));
    }

    [Fact]
    public async Task Add_rejects_parent_from_different_classifier()
    {
        ClassifierExists();
        ClassifierValueDto dto = Value();
        dto.ParentValueId = Guid.NewGuid();
        _values.Setup(r => r.GetClassifierValueByIdAsync(dto.ParentValueId.Value))
               .ReturnsAsync(new ClassifierValueDto { Id = dto.ParentValueId.Value, ClassifierId = Guid.NewGuid() });

        await Assert.ThrowsAsync<BadRequestException>(() => _service.AddClassifierValueAsync(dto));
    }

    [Fact]
    public async Task Add_rejects_duplicate_code()
    {
        ClassifierExists();
        ClassifierValueDto dto = Value();
        _values.Setup(r => r.FindClassifierValuesByCodeOrNameAsync(_classifierId, dto.Code, dto.Name))
               .ReturnsAsync(new[] { Value() });

        await Assert.ThrowsAsync<BadRequestException>(() => _service.AddClassifierValueAsync(dto));
    }

    [Fact]
    public async Task Add_succeeds_and_writes_value_added_history()
    {
        ClassifierExists();
        ClassifierValueDto dto = Value();
        ClassifierValueDto created = Value();
        _values.Setup(r => r.FindClassifierValuesByCodeOrNameAsync(_classifierId, dto.Code, dto.Name))
               .ReturnsAsync(Array.Empty<ClassifierValueDto>());
        _values.Setup(r => r.AddClassifierValueAsync(It.IsAny<ClassifierValueDto>())).ReturnsAsync(created);

        ClassifierValueDto result = await _service.AddClassifierValueAsync(dto);

        Assert.Equal(created.Id, result.Id);
        _history.Verify(h => h.AddClassifierHistoryAsync(It.Is<ClassifierHistoryDto>(
            x => x.Operation == "value_added" && x.ClassifierId == _classifierId)), Times.Once);
    }

    [Fact]
    public async Task Retire_active_value_writes_value_retired_history()
    {
        ClassifierValueDto existing = Value();
        _values.Setup(r => r.GetClassifierValueByIdAsync(existing.Id)).ReturnsAsync(existing);

        await _service.RetireClassifierValueAsync(_classifierId, existing.Id);

        _values.Verify(r => r.SetClassifierValueActiveAsync(existing.Id, false, It.IsAny<Guid?>()), Times.Once);
        _history.Verify(h => h.AddClassifierHistoryAsync(It.Is<ClassifierHistoryDto>(x => x.Operation == "value_retired")), Times.Once);
    }
}
