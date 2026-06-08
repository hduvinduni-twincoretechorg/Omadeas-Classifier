namespace Omadeas.Classifier.Core.DTOs;

/// <summary>
/// A classifier selection mode (lookup). Platform-seeded and read-only.
/// One of SINGLE_SELECT, MULTIPLE_SELECT, HIERARCHICAL. Spec §6.9.
/// </summary>
public class ClassifierSelectionModeDto
{
    public Guid Id { get; set; }

    /// <summary>Always null for platform-seeded modes; reserved for tenant-custom modes.</summary>
    public Guid? CompanyId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Denormalised flag — true for MULTIPLE_SELECT and HIERARCHICAL.</summary>
    public bool AllowsMultiple { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }
}
