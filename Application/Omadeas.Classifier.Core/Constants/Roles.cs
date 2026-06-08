namespace Omadeas.Classifier.Core.Constants
{
    /// <summary>
    /// Role claim values issued by the Auth service. These must match the values the Auth
    /// service puts in the token's role claim (see Jwt:Issuer / RoleClaimType in Program).
    /// Centralized here so a rename is a single edit.
    /// </summary>
    public static class Roles
    {
        /// <summary>Platform operators who may manage platform-owned data (lookups, platform classifiers).</summary>
        public const string PlatformAdmin = "platform_admin";
    }
}
