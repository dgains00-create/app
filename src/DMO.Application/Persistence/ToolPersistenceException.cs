namespace DMO.Application.Persistence;

/// <summary>
/// Typed reason for a canonical Tool persistence failure.
/// </summary>
public enum ToolPersistenceFailureReason
{
    /// <summary>
    /// A canonical Tool with the same (<c>tool_type</c>, <c>reference</c>, <c>lot</c>) identity
    /// tuple already exists: the unique index closed the concurrent race.
    /// </summary>
    DuplicateIdentity,
}

/// <summary>
/// Typed persistence failure raised by the Tool repository for canonical-Tool writes that violate
/// database integrity.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §5.5 (two-layer duplicate prevention) and §12.2 rule 5 (constraint
/// names mapped by <c>PostgresException.SqlState</c> + <c>ConstraintName</c>).
/// <para>
/// The application layer already performs the identity query before the insert and produces the
/// useful operator message; this typed failure exists for the losing concurrent insert, so the
/// service can name the existing canonical <c>tool_id</c> instead of surfacing a raw database error.
/// </para>
/// </remarks>
public sealed class ToolPersistenceException : Exception
{
    /// <summary>Creates the failure with its typed reason.</summary>
    public ToolPersistenceException(
        ToolPersistenceFailureReason reason,
        string message,
        Guid? existingToolId = null)
        : base(message)
    {
        Reason = reason;
        ExistingToolId = existingToolId;
    }

    /// <summary>Creates the failure with its typed reason, message and underlying cause.</summary>
    public ToolPersistenceException(
        ToolPersistenceFailureReason reason,
        string message,
        Exception innerException,
        Guid? existingToolId = null)
        : base(message, innerException)
    {
        Reason = reason;
        ExistingToolId = existingToolId;
    }

    /// <summary>The typed persistence failure reason.</summary>
    public ToolPersistenceFailureReason Reason { get; }

    /// <summary>The existing canonical Tool identity, when it could be read; never invented.</summary>
    public Guid? ExistingToolId { get; }
}
