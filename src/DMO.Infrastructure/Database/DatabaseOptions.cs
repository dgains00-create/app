namespace DMO.Infrastructure.Database;

/// <summary>
/// Database connection configuration, bound from application configuration/environment.
/// </summary>
/// <remarks>
/// <para>
/// The connection string is supplied by configuration or environment variables only.
/// No credential, host, user or password is ever defaulted, embedded or committed.
/// </para>
/// <para>
/// There is deliberately no fallback value. When the connection string is absent the
/// infrastructure fails loudly instead of silently substituting a default that could
/// look like a real environment.
/// </para>
/// </remarks>
public sealed class DatabaseOptions
{
    /// <summary>Configuration section name bound by the host.</summary>
    public const string SectionName = "Database";

    /// <summary>
    /// PostgreSQL connection string.
    /// </summary>
    /// <remarks>
    /// Supplied through user-secrets, environment variables or the deployment platform.
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Configuration key that carries the connection string.
    /// </summary>
    public const string ConnectionStringKey = $"{SectionName}:{nameof(ConnectionString)}";

    /// <summary>
    /// Environment-variable form of <see cref="ConnectionStringKey"/> as resolved by the
    /// default configuration builder (<c>Database__ConnectionString</c>).
    /// </summary>
    public const string ConnectionStringEnvironmentVariable = "Database__ConnectionString";
}
