namespace DMO.Domain.Controlo;

/// <summary>
/// One email template: subject, body and the applicable document type/context.
/// </summary>
/// <remarks>
/// Authority: P2-T05 contract §14 and §16.8. A template has a unique non-blank <c>name</c>, a
/// non-blank <c>subject</c> and a non-blank <c>body</c> stored verbatim — no placeholder syntax is
/// fixed and no parsing/substitution exists in P2-T05 (Q-PLACE). <see cref="DocumentType"/> is
/// <c>null</c> (generic template) or one of the three Beta document output families
/// (Q-DOCTYPE); a template carries no recipient-list association (routing is P2-T08's contract,
/// Q-ROUTE).
/// </remarks>
public sealed record EmailTemplate(
    EmailTemplateId EmailTemplateId,
    string Name,
    string Subject,
    string Body,
    EmailTemplateDocumentType? DocumentType,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);