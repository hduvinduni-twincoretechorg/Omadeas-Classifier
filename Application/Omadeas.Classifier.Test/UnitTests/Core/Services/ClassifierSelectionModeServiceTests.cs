using Moq;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Exceptions;
using Omadeas.Classifier.Core.Interfaces;
using Omadeas.Classifier.Core.Services;

namespace Omadeas.Classifier.Test.UnitTests.Core.Services;

public class ClassifierSelectionModeServiceTests
{
    private readonly Mock<IClassifierSelectionModeRepository> _repo = new();
    private readonly ClassifierSelectionModeService _service;

    public ClassifierSelectionModeServiceTests()
    {
        _service = new ClassifierSelectionModeService(_repo.Object);
    }

    private static ClassifierSelectionModeDto Mode(string code = "SINGLE_SELECT", string name = "Single Select")
        => new() { Id = Guid.NewGuid(), Code = code, Name = name, AllowsMultiple = false, IsActive = true };

    [Fact]
    public async Task GetById_throws_NotFound_when_missing()
    {
        _repo.Setup(r => r.GetClassifierSelectionModeByIdAsync(It.IsAny<Guid>()))
             .ReturnsAsync((ClassifierSelectionModeDto?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetClassifierSelectionModeByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Add_rejects_code_outside_allowed_set()
    {
        _repo.Setup(r => r.GetAllClassifierSelectionModesAsync(null))
             .ReturnsAsync(Array.Empty<ClassifierSelectionModeDto>());

        await Assert.ThrowsAsync<BadRequestException>(() => _service.AddClassifierSelectionModeAsync(Mode(code: "BOGUS")));
    }

    [Fact]
    public async Task Add_rejects_duplicate_code()
    {
        _repo.Setup(r => r.GetAllClassifierSelectionModesAsync(null))
             .ReturnsAsync(new[] { Mode() });

        await Assert.ThrowsAsync<BadRequestException>(() => _service.AddClassifierSelectionModeAsync(Mode()));
    }

    [Fact]
    public async Task Add_succeeds_for_valid_unique_mode()
    {
        ClassifierSelectionModeDto dto = Mode("MULTIPLE_SELECT", "Multiple Select");
        _repo.Setup(r => r.GetAllClassifierSelectionModesAsync(null))
             .ReturnsAsync(Array.Empty<ClassifierSelectionModeDto>());
        _repo.Setup(r => r.AddClassifierSelectionModeAsync(dto)).ReturnsAsync(dto);

        ClassifierSelectionModeDto result = await _service.AddClassifierSelectionModeAsync(dto);

        Assert.Equal("MULTIPLE_SELECT", result.Code);
        _repo.Verify(r => r.AddClassifierSelectionModeAsync(dto), Times.Once);
    }

    [Fact]
    public async Task Update_throws_NotFound_when_missing()
    {
        ClassifierSelectionModeDto dto = Mode();
        _repo.Setup(r => r.GetClassifierSelectionModeByIdAsync(dto.Id))
             .ReturnsAsync((ClassifierSelectionModeDto?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateClassifierSelectionModeAsync(dto));
    }

    [Fact]
    public async Task Delete_throws_NotFound_when_missing()
    {
        _repo.Setup(r => r.GetClassifierSelectionModeByIdAsync(It.IsAny<Guid>()))
             .ReturnsAsync((ClassifierSelectionModeDto?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteClassifierSelectionModeAsync(Guid.NewGuid()));
    }
}
