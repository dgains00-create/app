namespace DMO.Web.Auth;

/// <summary>
/// Raised when the Supabase Auth configuration is absent or invalid.
/// </summary>
/// <remarks>
/// This is a deliberate fail-fast condition mirroring the database fail-fast posture: the host
/// must never start against missing or invalid Supabase configuration.
/// </remarks>
public sealed class SupabaseConfigurationException : InvalidOperationException
{
    /// <summary>Creates the exception with an operator-facing message.</summary>
    public SupabaseConfigurationException(string message) : base(message)
    {
    }
}

/// <summary>
/// Configuration for the DEV/TEST Supabase Auth integration (ADMIN path only).
/// </summary>
/// <remarks>
/// <para>
/// No real key or password ever appears in code or committed configuration. Values are
/// supplied through <c>dotnet user-secrets</c> (local development) or environment variables
/// (runtime/deploy): <c>Supabase__ProjectUrl</c> and <c>Supabase__PublishableKey</c>.
/// </para>
/// <para>
/// The publishable key is the current Supabase key model for client-facing use
/// (<c>sb_publishable_…</c>). The legacy term "anon key" is not used. No <c>service_role</c>
/// and no secret key is ever used by P1-T02.
/// </para>
/// </remarks>
public sealed class SupabaseOptions
{
    /// <summary>Configuration section name bound by the host.</summary>
    public const string SectionName = "Supabase";

    /// <summary>Configuration key that carries the project URL.</summary>
    public const string ProjectUrlKey = $"{SectionName}:{nameof(ProjectUrl)}";

    /// <summary>Configuration key that carries the publishable key.</summary>
    public const string PublishableKeyKey = $"{SectionName}:{nameof(PublishableKey)}";

    /// <summary>Environment-variable form of <see cref="ProjectUrlKey"/>.</summary>
    public const string ProjectUrlEnvironmentVariable = "Supabase__ProjectUrl";

    /// <summary>Environment-variable form of <see cref="PublishableKeyKey"/>.</summary>
    public const string PublishableKeyEnvironmentVariable = "Supabase__PublishableKey";

    /// <summary>DEV/TEST Supabase project URL, e.g. <c>https://jixnteypqqrltsxgwzpv.supabase.co</c>.</summary>
    public string? ProjectUrl { get; set; }

    /// <summary>Supabase publishable key (never committed; user-secrets or environment only).</summary>
    public string? PublishableKey { get; set; }

    /// <summary>
    /// Validates the configuration and throws <see cref="SupabaseConfigurationException"/>
    /// when a required value is absent or invalid.
    /// </summary>
    /// <remarks>
    /// There is deliberately no fallback value. A missing or invalid configuration fails
    /// loudly instead of silently degrading to an unauthenticated mode.
    /// </remarks>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ProjectUrl)
            || !Uri.TryCreate(ProjectUrl, UriKind.Absolute, out var parsed)
            || parsed.Scheme is not ("https" or "http"))
        {
            throw new SupabaseConfigurationException(
                $"Supabase project URL is not configured. Set '{ProjectUrlKey}' " +
                $"(configuration or user-secrets) or the '{ProjectUrlEnvironmentVariable}' " +
                "environment variable. No default is assumed.");
        }

        if (string.IsNullOrWhiteSpace(PublishableKey))
        {
            throw new SupabaseConfigurationException(
                $"Supabase publishable key is not configured. Set '{PublishableKeyKey}' " +
                $"(configuration or user-secrets) or the '{PublishableKeyEnvironmentVariable}' " +
                "environment variable. No default is assumed.");
        }
    }
}