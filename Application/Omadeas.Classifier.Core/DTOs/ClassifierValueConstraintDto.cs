namespace Omadeas.Classifier.Core.DTOs;

/// <summary>
/// An inter-value rule across classifiers. Advisory in v2. Spec §6.4.
/// </summary>
public class ClassifierValueConstraintDto
{
    public Guid Id { get; set; }

    /// <summary>The trigger classifier.</summary>
    public Guid SourceClassifierId { get; set; }

    /// <summary>The trigger value (must belong to SourceClassifierId — R8).</summary>
    public Guid SourceValueId { get; set; }

    /// <summary>The affected classifier.</summary>
    public Guid TargetClassifierId { get; set; }

    /// <summary>The affected value (must belong to TargetClassifierId — R9).</summary>
    public Guid TargetValueId { get; set; }

    /// <summary>'REQUIRES' | 'PROHIBITS' | 'WARNS'.</summary>
    public string ConstraintType { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }
}
