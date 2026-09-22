namespace DMO.Application.Persistence;

/// <summary>
/// Typed reason for a <see cref="UserPersistenceException"/>.
/// </summary>
public enum UserPersistenceFailureReason
{
    /// <summary>The write violated the unique company number (a concurrent writer won the race).</summary>
    DuplicateCompanyNumber,

    /// <summary>The write violated the unique provider subject (a concurrent writer already mapped it).</summary>
    DuplicateProviderSubject,

    /// <summary>The write referenced a Template that does not exist (FK race).</summary>
    InvalidTemplateReference,
}

/// <summary>
/// Typed persistence failure raised by the repository layer for USER writes that violate
/// database integrity constraints.
/// </summary>
/// <remarks>
/// <para>
/// The persistence layer maps constraint violations (unique company number, unique provider
/// subject, invalid Template FK) onto this typed domain failure so Application services can
/// drive the accepted compensation posture (P1-T05 §13) without referencing EF/Npgsql.
/// </para>
/// <para>
/// Optimistic-concurrency conflicts remain the separate
/// <see cref="ConcurrencyConflictException"/>; anything not a constraint violation propagates
/// unchanged (genuine infrastructure failure → the transport layer reports it plainly and the
/// operator retries).
/// </para>
/// </remarks>
public sealed class UserPersistenceException : Exception
{
    /// <summary>Creates the failure with its typed reason.</summary>
    public UserPersistenceException(
        UserPersistenceFailureReason reason,
        string message)
        : base(message)
    {
        Reason = reason;
    }

    /// <summary>Creates the failure with a typed reason, message and underlying cause.</summary>
    public UserPersistenceException(
        UserPersistenceFailureReason reason,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        Reason = reason;
    }

    /// <summary>The typed persistence failure reason.</summary>
    public UserPersistenceFailureReason Reason { get; }
}