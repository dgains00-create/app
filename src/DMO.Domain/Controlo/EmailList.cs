namespace DMO.Domain.Controlo;

/// <summary>
/// One named email recipient list with its complete recipient set.
/// </summary>
/// <remarks>
/// Authority: P2-T05 contract §13 and §16.6. A list has a unique non-blank <c>name</c> (a named
/// list must be unambiguously addressable — Q-NAME) and an associated recipient set that is always
/// replaced as a whole (replace-all semantics; no partial list state is ever persisted — §18.6).
/// Recipient addresses are never hardcoded in application code (SET8/AC-F6). An update outside the
/// list row is version-guarded by <see cref="Version"/>.
/// </remarks>
public sealed record EmailList(
    EmailListId EmailListId,
    string Name,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<EmailRecipient> Recipients);