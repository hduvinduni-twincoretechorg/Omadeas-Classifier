namespace Omadeas.Classifier.Core.DTOs;

/// <summary>
/// An allowed value for a classifier. Spec §6.2.
/// </summary>
public class ClassifierValueDto
{
    public Guid Id { get; set; }

    public Guid ClassifierId { get; set; }

    /// <summary>Self-FK for HIERARCHICAL classifiers; null for flat classifiers / tree roots.</summary>
    public Guid? ParentValueId { get; set; }

    /// <summary>Machine code, UPPER_SNAKE_CASE, unique per classifier.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display label, unique per classifier.</summary>
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Sort order within the classifier's value list. Defaults to 100.</summary>
    public int Order { get; set; } = 100;

    /// <summary>Hex colour for UI chips (e.g. #ef4444).</summary>
    public string? Colour { get; set; }

    /// <summary>Optional unicode glyph / short string.</summary>
    public string? Icon { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }
}
