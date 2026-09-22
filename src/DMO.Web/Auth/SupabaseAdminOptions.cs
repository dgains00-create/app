namespace DMO.Web.Auth;

/// <summary>
/// Configuration for the server-only privileged Supabase Auth Admin boundary (P1-T05).
/// </summary>
/// <remarks>
/// <para>
/// This options type carries the <b>service-role secret</b> used exclusively by
/// <see cref="SupabaseAdminUserService"/> (invite, get, paginated find, update email, delete).
/// The value comes from <c>dotnet user-secrets</c> (local development) or an environment
/// variable (runtime/deploy): <c>SupabaseAdmin__ServiceRoleKey</c>. It is never committed,
/// never logged, never exposed to the browser/Razor/JS/HTTP responses, and never used by the
/// normal login path (<see cref="SupabaseOptions"/> keeps only the publishable key).
/// </para>
/// <para>
/// Eager validation mirrors the publishable-key posture: the host must never start against a
/// missing service-role secret when the administration surface is registered.
/// </para>
/// </remarks>
public sealed class SupabaseAdminOptions
{
    /// <summary>Configuration section name bound by the host.</summary>
    public const string SectionName = "SupabaseAdmin";

    /// <summary>Configuration key that carries the service-role secret.</summary>
    public const string ServiceRoleKeyKey = $"{SectionName}:{nameof(ServiceRoleKey)}";

    /// <summary>Environment-variable form of <see cref="ServiceRoleKeyKey"/>.</summary>
    public const string ServiceRoleKeyEnvironmentVariable = "SupabaseAdmin__ServiceRoleKey";

    /// <summary>The server-only Supabase service-role secret (never committed; user-secrets or environment only).</summary>
    public string? ServiceRoleKey { get; set; }

    /// <summary>
    /// Validates the configuration and throws <see cref="SupabaseConfigurationException"/>
    /// when the service-role secret is absent.
    /// </summary>
    /// <remarks>
    /// There is deliberately no fallback value; a missing secret fails loudly instead of
    /// silently degrading the administration surface.
    /// </remarks>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ServiceRoleKey))
        {
            throw new SupabaseConfigurationException(
                $"Supabase Admin service role key is not configured. Set '{ServiceRoleKeyKey}' " +
                $"(configuration or user-secrets) or the '{ServiceRoleKeyEnvironmentVariable}' " +
                "environment variable. No default is assumed. This secret is server-only and must " +
                "never be exposed to the browser or logged.");
        }
    }
}