namespace DMO.Application.Persistence;

/// <summary>
/// Typed Boquilhas persistence failure of the <c>IBoquilhasRepository</c> writes (contract §8.2
/// binding rules and §7.2/§7.4 backstop discipline).
/// </summary>
/// <remarks>
/// <para>
/// The Boquilhas repositories map PostgreSQL constraint violations onto these typed failures so a
/// validator-passing combination can never surface as a generic 500:
/// </para>
/// <list type="bullet">
/// <item>SQLSTATE <c>23505 unique_violation</c> on <c>IX_boquilhas_active_bq_id</c> or
/// <c>IX_boquilhas_active_tool_id</c> → <see cref="BoquilhasPersistenceFailureReason.ActiveAggregateExists"/>
/// (the exact scoped mapping of §7.2 — <b>no other</b> 23505 source maps to this domain result);</item>
/// <item>SQLSTATE <c>23514 check_violation</c> on a P2-T07 CHECK → the <b>same validator token</b> the
/// validator raises first (carried by <see cref="ConstraintViolation"/>);</item>
/// <item>SQLSTATE <c>23503 foreign_key_violation</c> on the anchor/reference FKs → the typed
/// anchor tokens (<see cref="BqContextNotFound"/>/<see cref="ToolNotFound"/>/<see cref="RepairerNotFound"/>).</item>
/// </list>
/// <para>
/// The balance-relative and lifecycle refusals that the repository re-asserts authoritatively
/// <b>inside</b> its write transaction (replay computed over the ledger loaded in the same
/// transaction — §17.2/§19.2/§23) are also typed here. General optimistic-concurrency conflicts use
/// <see cref="ConcurrencyConflictException"/> (mapped to 409 <c>stale-version</c> by the service).
/// </para>
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
    /// <see cref="BoquilhasPersistenceFailureReason.ConstraintViolation"/> and the anchor/tool/
    /// repairer-token reasons; always one of the §12.2 closed set).
    /// </summary>
    public string? ValidatorToken { get; }
}

/// <summary>The typed Boquilhas persistence failure reasons.</summary>
public enum BoquilhasPersistenceFailureReason
{
    /// <summary>
    /// 23505 on <c>IX_boquilhas_active_bq_id</c>/<c>IX_boquilhas_active_tool_id</c> — a concurrent
    /// create/reopen committed an ACTIVE aggregate on the same anchor first; the whole transaction
    /// rolled back (the DB index is the race-safe concurrency authority, §7.2).
    /// </summary>
    ActiveAggregateExists,

    /// <summary>23514 on a P2-T07 CHECK — the same validator token the validator raises first.</summary>
    ConstraintViolation,

    /// <summary>The supplied <c>bq_id</c> is not a real <c>bq_contexts</c> row (or it vanished).</summary>
    BqContextNotFound,

    /// <summary>The supplied standalone <c>tool_id</c> is not a real canonical Tool (or it vanished).</summary>
    ToolNotFound,

    /// <summary>The supplied standalone <c>tool_id</c> is a Tool whose type is not <c>BQ</c>.</summary>
    ToolTypeMismatch,

    /// <summary>The supplied <c>repairer_id</c> is not a real register row (RESTRICT FK backstop).</summary>
    RepairerNotFound,

    /// <summary>In-transaction re-assertion: the aggregate is already <c>closed</c>; nothing written.</summary>
    AlreadyClosed,

    /// <summary>In-transaction re-assertion: the aggregate is not <c>closed</c>; nothing written.</summary>
    NotClosed,

    /// <summary>In-transaction re-assertion: another aggregate sharing the anchor holds a later close.</summary>
    NotLastClosed,

    /// <summary>In-transaction re-assertion: the aggregate is <c>closed</c>; no mutation accepted.</summary>
    AggregateClosed,

    /// <summary>In-transaction re-assertion: a second Início append is refused.</summary>
    OnlyOneInicio,

    /// <summary>In-transaction replay re-assertion: the Saída quantity exceeds Disponível.</summary>
    SaidaExceedsAvailable,

    /// <summary>In-transaction replay re-assertion: the Irreparável quantity exceeds Em reparação.</summary>
    IrreparavelExceedsInRepair,
}