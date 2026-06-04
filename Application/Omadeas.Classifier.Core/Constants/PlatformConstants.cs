namespace Omadeas.Classifier.Core.Constants;

/// <summary>
/// Platform-wide reserved identifiers and enum values shared across the Classifier service.
/// </summary>
public static class PlatformConstants
{
    /// <summary>
    /// Reserved company_id used by platform-seeded (source='platform') rows. Spec §6.1.
    /// </summary>
    public static readonly Guid PlatformTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Stand-in principal id recorded on audit/history rows written without an authenticated
    /// principal (seed/bootstrap/system writes). occurred_by is a cross-service soft FK, so an
    /// out-of-band value is acceptable until a real SYSTEM principal / auth context is wired in.
    /// </summary>
    public static readonly Guid SystemPrincipalId = Guid.Empty;

    /// <summary>Source flag values. Spec §6.1 / §13 #3.</summary>
    public static class Source
    {
        public const string Platform = "platform";
        public const string Tenant = "tenant";
    }
}
