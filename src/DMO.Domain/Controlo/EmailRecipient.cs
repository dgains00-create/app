namespace DMO.Domain.Controlo;

/// <summary>
/// One recipient address of one named email list.
/// </summary>
/// <remarks>
/// Authority: P2-T05 contract §13.2 and §16.7. Each recipient is exactly one <c>address</c>;
/// validation is the minimal unbroken address shape (<c>ADDRESS_INVALID</c>), no full RFC
/// validation is invented (Q-ADDR). The same address may appear in several lists; one address
/// occurs once per list (<c>UNIQUE (email_list_id, address)</c>). No display name, no position and
/// no ordering column exists (S20/Q-ORDER); the deterministic technical order is <c>address ASC</c>.
/// The row carries no <c>version</c>: the parent list's version protects the set.
/// </remarks>
public sealed record EmailRecipient(
    Guid EmailListRecipientId,
    EmailListId EmailListId,
    string Address,
    DateTimeOffset CreatedAt);