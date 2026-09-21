using Npgsql;

namespace DMO.Infrastructure.Database;

/// <summary>
/// Raised when the database configuration is absent or invalid.
/// </summary>
/// <remarks>
/// This is a deliberate fail-fast condition. The application must never start against a
/// guessed, implicit or production-looking default connection.
/// </remarks>
public sealed class DatabaseConfigurationException : InvalidOperationException
{
    /// <summary>Creates the exception with an operator-facing message.</summary>
    public DatabaseConfigurationException(string message) : base(message)
    {
    }

    /// <summary>Creates the exception with an operator-facing message and an inner cause.</summary>
    public DatabaseConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Resolves and validates the PostgreSQL connection string.
/// </summary>
/// <remarks>
/// This is the single place where the connection string is obtained, so that every
/// consumer (connection factory, migration runner) fails with the same clear message
/// rather than each inventing its own behaviour.
/// </remarks>
public sealed class DatabaseConnectionResolver
{
    private readonly DatabaseOptions _options;

    /// <summary>Creates the resolver over the bound options.</summary>
    public DatabaseConnectionResolver(DatabaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <summary>
    /// Returns the configured PostgreSQL connection string, validated.
    /// </summary>
    /// <exception cref="DatabaseConfigurationException">
    /// Thrown when the connection string is missing, blank, or not a parseable PostgreSQL
    /// connection string.
    /// </exception>
    public string GetConnectionString()
    {
        var configured = _options.ConnectionString;

        if (string.IsNullOrWhiteSpace(configured))
        {
            throw new DatabaseConfigurationException(
                $"Database connection string is not configured. Set '{DatabaseOptions.ConnectionStringKey}' " +
                $"(configuration or user-secrets) or the '{DatabaseOptions.ConnectionStringEnvironmentVariable}' " +
                "environment variable. No default connection is assumed.");
        }

        // Validate that Npgsql can actually interpret it, and that a host and database are present.
        NpgsqlConnectionStringBuilder builder;

        try
        {
            builder = new NpgsqlConnectionStringBuilder(configured);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            throw new DatabaseConfigurationException(
                $"Database connection string in '{DatabaseOptions.ConnectionStringKey}' is not a valid " +
                $"PostgreSQL connection string: {ex.Message}",
                ex);
        }

        if (string.IsNullOrWhiteSpace(builder.Host))
        {
            throw new DatabaseConfigurationException(
                $"Database connection string in '{DatabaseOptions.ConnectionStringKey}' does not specify a Host.");
        }

        if (string.IsNullOrWhiteSpace(builder.Database))
        {
            throw new DatabaseConfigurationException(
                $"Database connection string in '{DatabaseOptions.ConnectionStringKey}' does not specify a Database.");
        }

        return builder.ConnectionString;
    }
}
