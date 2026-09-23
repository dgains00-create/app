namespace DMO.Application.Persistence;

/// <summary>
/// Typed decision-insert failure of the P2-T06 repositories (contract §8.2 binding rules).
/// </summary>
/// <remarks>
/// The P2-T06 decision write maps PostgreSQL constraint violations onto these typed failures so a
/// validator-passing combination can never surface as a generic 500: SQLSTATE <c>23514</c> on the
/// reason CHECK → the same validator token (<c>REJECT_REASON_REQUIRED</c>/
/// <c>REOPEN_REASON_REQUIRED</c>), and a RESTRICT FK violation (a vanished Peso or user) → the
/// dependency refusal. Only the decision-logic failures live here; general optimistic-concurrency
/// conflicts use <see cref="ConcurrencyConflictException"/>.</remarks>
public sealed class PesoReviewPersistenceException : Exception
{
    /// <summary>Creates the typed failure.</summary>
    public PesoReviewPersistenceException(PesoReviewPersistenceFailureReason reason, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Reason = reason;
    }

    /// <summary>The typed failure reason.</summary>
    public PesoReviewPersistenceFailureReason Reason { get; }
}

/// <summary>The typed decision-insert failure reasons.</summary>
public enum PesoReviewPersistenceFailureReason
{
    /// <summary>The reason CHECK backstop was hit (the validator already refuses it first).</summary>
    ReasonRequired,

    /// <summary>A referenced Peso or user vanished (RESTRICT FK backstop).</summary>
    DependencyExists,
}