namespace DMO.Web.Startup;

/// <summary>
/// Configuration for the single-ADMIN bootstrap command.
/// </summary>
/// <remarks>
/// <para>
/// Inputs are exactly three (env/user-secrets only, never committed):
/// <c>AdminBootstrap__Email</c>, <c>AdminBootstrap__DisplayName</c> and
/// <c>AdminBootstrap__AuthIdentityId</c> (the exact provider subject of the existing
/// Supabase Auth ADMIN identity, already created manually by the operator in DEV/TEST).
/// </para>
/// <para>
/// The bootstrap performs no provider call: the identity is asserted by the supplied
/// subject, never verified through any Supabase API, and no <c>service_role</c>, management
/// token or ADMIN password is used.
/// </para>
/// </remarks>
public sealed class AdminBootstrapOptions
{
    /// <summary>Configuration section name bound by the host.</summary>
    public const string SectionName = "AdminBootstrap";

    /// <summary>Configuration key for the ADMIN email.</summary>
    public const string EmailKey = $"{SectionName}:{nameof(Email)}";

    /// <summary>Configuration key for the ADMIN display name.</summary>
    public const string DisplayNameKey = $"{SectionName}:{nameof(DisplayName)}";

    /// <summary>Configuration key for the exact provider subject of the existing Supabase ADMIN identity.</summary>
    public const string AuthIdentityIdKey = $"{SectionName}:{nameof(AuthIdentityId)}";

    /// <summary>The ADMIN email (provider credential carrier / login identifier).</summary>
    public string? Email { get; set; }

    /// <summary>The display name for the single ADMIN row.</summary>
    public string? DisplayName { get; set; }

    /// <summary>The exact provider subject (UUID) of the existing Supabase Auth ADMIN identity.</summary>
    public string? AuthIdentityId { get; set; }
}