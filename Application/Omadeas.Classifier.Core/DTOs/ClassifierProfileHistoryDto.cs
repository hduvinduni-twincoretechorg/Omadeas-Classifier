namespace Omadeas.Classifier.Core.DTOs;

/// <summary>
/// An append-only profile definition audit event. Spec §14.9.
/// </summary>
public class ClassifierProfileHistoryDto
{
    public Guid Id { get; set; }

    public Guid ProfileId { get; set; }

    /// <summary>One of the profile-history operations (created, renamed, scope_changed, member_added, …).</summary>
    public string Operation { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; }

    /// <summary>The principal whose action caused the change (soft FK to principals).</summary>
    public Guid OccurredBy { get; set; }

    public string Summary { get; set; } = string.Empty;

    public string? Detail { get; set; }

    /// <summary>Raw JSON before/after snapshot for forensics.</summary>
    public string? Payload { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }
}
