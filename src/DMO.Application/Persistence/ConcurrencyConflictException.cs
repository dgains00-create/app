namespace DMO.Application.Persistence;

/// <summary>
/// Typed optimistic-concurrency conflict raised when a persisted write carries a stale row
/// version.
/// </summary>
/// <remarks>
/// <para>
/// Raised by the persistence layer (mapping the EF <c>DbUpdateConcurrencyException</c>) so
/// services and endpoints can translate it into a 409 Conflict. A stale write is never
/// silently overwritten: the write is rejected and the caller must reload and retry.
/// </para>
/// <para>
/// This is the domain-level concurrency contract; persistence mechanisms (EF concurrency
/// tokens, <c>version = version + 1 WHERE version = @expected</c>) stay in
/// <c>DMO.Infrastructure</c>.
/// </para>
/// </remarks>
public sealed class ConcurrencyConflictException : InvalidOperationException
{
    /// <summary>Creates the exception with an operator-facing message.</summary>
    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and an underlying cause.</summary>
    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}