using Moq;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Exceptions;
using Omadeas.Classifier.Core.Interfaces;
using Omadeas.Classifier.Core.Services;

namespace Omadeas.Classifier.Test.UnitTests.Core.Services;

public class ClassifierProfileMemberServiceTests
{
    private readonly Mock<IClassifierProfileMemberRepository> _members = new();
    private readonly Mock<IClassifierProfileRepository> _profiles = new();
    private readonly Mock<IClassifierRepository> _classifiers = new();
    private readonly Mock<IClassifierProfileHistoryRepository> _history = new();
    private readonly ClassifierProfileMemberService _service;

    private readonly Guid _profileId = Guid.NewGuid();
    private readonly Guid _classifierId = Guid.NewGuid();

    public ClassifierProfileMemberServiceTests()
    {
        _service = new ClassifierProfileMemberService(
            _members.Object, _profiles.Object, _classifiers.Object, _history.Object);
    }

    private void ProfileExists()
        => _profiles.Setup(r => r.GetClassifierProfileByIdAsync(_profileId))
                    .ReturnsAsync(new ClassifierProfileDto { Id = _profileId, Code = "RISK_GOV", Name = "Risk Governance" });

    private void ClassifierExists()
        => _classifiers.Setup(r => r.GetClassifierByIdAsync(_classifierId))
                       .ReturnsAsync(new ClassifierDto { Id = _classifierId, Code = "PRIORITY", Name = "Priority" });

    private ClassifierProfileMemberDto Member()
        => new() { Id = Guid.NewGuid(), ProfileId = _profileId, ClassifierId = _classifierId, IsRequired = false, Order = 10 };

    [Fact]
    public async Task GetMembers_throws_NotFound_when_profile_missing()
    {
        _profiles.Setup(r => r.GetClassifierProfileByIdAsync(_profileId)).ReturnsAsync((ClassifierProfileDto?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetClassifierProfileMembersAsync(_profileId));
    }

    [Fact]
    public async Task Add_throws_NotFound_when_profile_missing()
    {
        _profiles.Setup(r => r.GetClassifierProfileByIdAsync(_profileId)).ReturnsAsync((ClassifierProfileDto?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => _service.AddClassifierProfileMemberAsync(Member()));
    }

    [Fact]
    public async Task Add_rejects_when_classifier_missing()
    {
        ProfileExists();
        _classifiers.Setup(r => r.GetClassifierByIdAsync(_classifierId)).ReturnsAsync((ClassifierDto?)null);
        await Assert.ThrowsAsync<BadRequestException>(() => _service.AddClassifierProfileMemberAsync(Member()));
    }

    [Fact]
    public async Task Add_rejects_duplicate_classifier_in_profile()
    {
        ProfileExists();
        ClassifierExists();
        _members.Setup(r => r.GetClassifierProfileMembersAsync(_profileId)).ReturnsAsync(new[] { Member() });

        await Assert.ThrowsAsync<BadRequestException>(() => _service.AddClassifierProfileMemberAsync(Member()));
    }

    [Fact]
    public async Task Add_succeeds_and_writes_member_added_history()
    {
        ProfileExists();
        ClassifierExists();
        ClassifierProfileMemberDto created = Member();
        _members.Setup(r => r.GetClassifierProfileMembersAsync(_profileId)).ReturnsAsync(Array.Empty<ClassifierProfileMemberDto>());
        _members.Setup(r => r.AddClassifierProfileMemberAsync(It.IsAny<ClassifierProfileMemberDto>())).ReturnsAsync(created);

        await _service.AddClassifierProfileMemberAsync(Member());

        _history.Verify(h => h.AddClassifierProfileHistoryAsync(It.Is<ClassifierProfileHistoryDto>(
            x => x.Operation == "member_added" && x.ProfileId == _profileId)), Times.Once);
    }

    [Fact]
    public async Task Delete_writes_member_removed_history()
    {
        ClassifierProfileMemberDto existing = Member();
        _members.Setup(r => r.GetClassifierProfileMemberByIdAsync(existing.Id)).ReturnsAsync(existing);

        await _service.DeleteClassifierProfileMemberAsync(_profileId, existing.Id);

        _members.Verify(r => r.DeleteClassifierProfileMemberAsync(existing.Id), Times.Once);
        _history.Verify(h => h.AddClassifierProfileHistoryAsync(It.Is<ClassifierProfileHistoryDto>(x => x.Operation == "member_removed")), Times.Once);
    }

    [Fact]
    public async Task GetById_throws_NotFound_when_member_belongs_to_other_profile()
    {
        ClassifierProfileMemberDto other = Member();
        other.ProfileId = Guid.NewGuid();
        _members.Setup(r => r.GetClassifierProfileMemberByIdAsync(other.Id)).ReturnsAsync(other);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetClassifierProfileMemberByIdAsync(_profileId, other.Id));
    }
}
