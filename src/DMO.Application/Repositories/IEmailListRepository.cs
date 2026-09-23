using DMO.Domain.Controlo;

namespace DMO.Application.Repositories;

/// <summary>
/// The single email-list repository contract (P2-T05 contract §20.2, exact).
/// </summary>
/// <remarks>
/// A list update carries the <b>complete</b> recipient set and replaces it atomically inside one
/// transaction — no partial list state is ever observable (§18.6; SET4/SET5). The parent list's
/// version protects the recipient set (no per-recipient version). The unique-name violation
/// (<c>email_lists_name_key</c>) maps to the <c>duplicate-name</c> refusal; a delete blocked by a
/// future dependent maps to <c>dependency-exists</c> (RESTRICT backstop).
/// </remarks>
public interface IEmailListRepository
{
    /// <summary>Reads one list with its complete recipient set, or <c>null</c>.</summary>
    Task<EmailList?> GetByIdAsync(Guid emailListId, CancellationToken cancellationToken);

    /// <summary>Lists every list with its recipient count, in deterministic name order.</summary>
    Task<IReadOnlyList<EmailList>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Inserts the list and its complete recipient set in one transaction (version = 1).</summary>
    Task<EmailList> CreatedAsync(
        EmailList list,
        IReadOnlyList<EmailRecipient> recipients,
        CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the list row and its complete recipient set in one transaction (version += 1).
    /// </summary>
    Task<EmailList> UpdatedAsync(
        EmailList list,
        IReadOnlyList<EmailRecipient> recipients,
        CancellationToken cancellationToken);

    /// <summary>Deletes the list in one transaction (version-guarded; RESTRICT backstop).</summary>
    Task DeletedAsync(Guid emailListId, int expectedVersion, CancellationToken cancellationToken);
}