namespace Omadeas.Classifier.Core.DTOs;

/// <summary>
/// The selection rules for a classifier (1:1 with classifier). Spec §6.3.
/// </summary>
public class ClassifierValuePolicyDto
{
    public Guid Id { get; set; }

    public Guid ClassifierId { get; set; }

    public Guid SelectionModeId { get; set; }

    public int? MinSelected { get; set; }

    public int? MaxSelected { get; set; }

    public bool IsRequired { get; set; }

    /// <summary>Optional auto-assigned value; must belong to the same classifier (R5).</summary>
    public Guid? DefaultValueId { get; set; }

    public bool AllowCustomValues { get; set; }

    public bool RequiresReasonOnChange { get; set; }

    /// <summary>'manual' | 'computed' | 'suggested'. v2 implements only 'manual'.</summary>
    public string ComputationMode { get; set; } = "manual";

    /// <summary>Calc-Engine soft FK; NULL in v2.</summary>
    public Guid? DefaultCalculationId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }
}
