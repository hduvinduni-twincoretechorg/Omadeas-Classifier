namespace Omadeas.Classifier.Core.DTOs;

/// <summary>
/// A classifier's membership in a profile (junction). Spec §6.6.
/// </summary>
public class ClassifierProfileMemberDto
{
    public Guid Id { get; set; }

    public Guid ProfileId { get; set; }

    public Guid ClassifierId { get; set; }

    /// <summary>Per-profile override of the classifier's policy is_required.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Display order within the profile. Defaults to 100.</summary>
    public int Order { get; set; } = 100;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }
}
