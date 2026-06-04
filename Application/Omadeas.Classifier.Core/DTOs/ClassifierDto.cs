namespace Omadeas.Classifier.Core.DTOs;

/// <summary>
/// A classifier definition — a named controlled vocabulary. Spec §6.1.
/// </summary>
public class ClassifierDto
{
    public Guid Id { get; set; }

    /// <summary>Owning tenant; PLATFORM_TENANT_ID for platform-seeded classifiers.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>'platform' or 'tenant'.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Machine code, UPPER_SNAKE_CASE, unique per company.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display label, unique per company.</summary>
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Sort order in the Available list. Defaults to 100.</summary>
    public int Order { get; set; } = 100;

    /// <summary>Anchor type codes this classifier applies to. Null/empty = any type.</summary>
    public List<string>? AppliesToNodeTypes { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }
}
