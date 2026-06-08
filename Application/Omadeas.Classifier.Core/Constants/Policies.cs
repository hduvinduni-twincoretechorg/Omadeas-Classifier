namespace Omadeas.Classifier.Core.Constants
{
    /// <summary>
    /// Authorization policy names. Registered in Program.AddAuthentication and applied to
    /// endpoints via [Authorize(Policy = Policies.&lt;Name&gt;)].
    /// </summary>
    public static class Policies
    {
        /// <summary>
        /// Platform-team only. Guards writes to platform-owned data (e.g. the selection-mode
        /// lookup). Satisfied by the <see cref="Roles.PlatformAdmin"/> role.
        /// </summary>
        public const string PlatformTeam = "PlatformTeam";
    }
}
