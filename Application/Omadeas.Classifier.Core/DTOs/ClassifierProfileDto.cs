namespace Omadeas.Classifier.Core.DTOs;

/// <summary>
/// A named bundle of classifiers, attached to nodes to scope which classifiers apply. Spec §6.5.
/// </summary>
public class ClassifierProfileDto
{
    public Guid Id { get; set; }

    /// <summary>Owning tenant; PLATFORM_TENANT_ID for platform-seeded profiles.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>'platform' or 'tenant'.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Machine code, UPPER_SNAKE_CASE, unique per company.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display label, unique per company.</summary>
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Display order. Defaults to 100.</summary>
    public int Order { get; set; } = 100;

    /// <summary>Default inheritance behaviour when attached: 'self' or 'subtree'.</summary>
    public string ScopeDefault { get; set; } = "subtree";

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }
}
