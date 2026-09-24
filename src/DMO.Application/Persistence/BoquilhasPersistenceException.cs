namespace DMO.Application.Persistence;

/// <summary>
/// Typed Boquilhas persistence failure of the <c>IBoquilhasRepository</c> writes (the OWNER
/// CLARIFICATION register model).
/// </summary>
/// <remarks>
/// <para>
/// The Boquilhas repository maps PostgreSQL constraint violations onto these typed failures so a
/// validator-passing combination can never surface as a generic 500:
/// </para>
/// <list type="bullet">
/// <item>SQLSTATE <c>23505 unique_violation</c> on the register's <c>bq_id</c> unique key →
/// <see cref="BoquilhasPersistenceFailureReason.RegisterExists"/> (one register per production/BQ
/// context, no lifecycle machinery);</item>
/// <item>SQLSTATE <c>23514 check_violation</c> on a P2-T07 CHECK → the <b>same validator token</b>
/// the validator raises first (carried by <see cref="ConstraintViolation"/>);</item>
/// <item>SQLSTATE <c>23503 foreign_key_violation</c> on the anchor/reference FKs → the typed
/// anchor tokens (<see cref="BqContextNotFound"/>/<see cref="RepairerNotFound"/>).</item>
/// </list>
/// <para>
/// <b>Superseded (Owner clarification):</b> <c>ActiveAggregateExists</c> (with its scoped 23505
/// mapping on the removed ACTIVE partial unique indexes) and the lifecycle/balance refusal reasons
/// (AlreadyClosed/NotClosed/NotLastClosed/AggregateClosed/OnlyOneInicio/SaidaExceedsAvailable/
/// IrreparavelExceedsInRepair) are REMOVED. General optimistic-concurrency conflicts (the
/// per-movement edit token) use <see cref="ConcurrencyConflictException"/> (409
/// <c>stale-version</c>).</para>
/// </remarks>
public sealed class BoquilhasPersistenceException : Exception
{
    /// <summary>Creates the typed failure.</summary>
    public BoquilhasPersistenceException(
        BoquilhasPersistenceFailureReason reason,
        string message,
        Exception? innerException = null,
        string? validatorToken = null)
        : base(message, innerException)
    {
        Reason = reason;
        ValidatorToken = validatorToken;
    }

    /// <summary>The typed failure reason.</summary>
    public BoquilhasPersistenceFailureReason Reason { get; }

    /// <summary>
    /// The exact validator transport token of the refused fact (set only for
    /// <see cref="BoquilhasPersistenceFailureReason.ConstraintViolation"/>; always one of the
    /// closed validation codes).
    /// </summary>
    public string? ValidatorToken { get; }
}

/// <summary>The typed Boquilhas persistence failure reasons.</summary>
public enum BoquilhasPersistenceFailureReason
{
    /// <summary>
    /// 23505 on the register's <c>bq_id</c> unique key — a register for this production/BQ context
    /// already exists; nothing written.
    /// </summary>
    RegisterExists,

    /// <summary>23514 on a P2-T07 CHECK — the same validator token the validator raises first.</summary>
    ConstraintViolation,

    /// <summary>The supplied <c>bq_id</c> is not a real <c>bq_contexts</c> row (or it vanished).</summary>
    BqContextNotFound,

    /// <summary>The supplied <c>repairer_id</c> is not a real register row (RESTRICT FK backstop).</summary>
    RepairerNotFound,
}