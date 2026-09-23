namespace DMO.Infrastructure.Persistence.Entities;

/// <summary>
/// Persistence entity for the <c>email_templates</c> table: one email template.
/// </summary>
/// <remarks>
/// Authority: P2-T05 contract §16.8. <see cref="Name"/> is unique
/// (<c>email_templates_name_key</c>); <see cref="Subject"/> and <see cref="Body"/> are trimmed
/// non-blank; <see cref="Body"/> is stored <b>verbatim</b> — no placeholder parsing or
/// substitution exists in P2-T05 (Q-PLACE). <see cref="DocumentType"/> is <c>NULL</c> (generic) or
/// one of <c>peso</c>/<c>pegamentos</c>/<c>resumo</c> (CHECK, Q-DOCTYPE). No recipient-list
/// association: routing is P2-T08's contract (Q-ROUTE).
/// </remarks>
public sealed class EmailTemplateEntity
{
    /// <summary>Primary key (backend-allocated).</summary>
    public Guid EmailTemplateId { get; set; }

    /// <summary>The unique template name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The template subject.</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>The template body (verbatim text).</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>The applicable document type token, or <c>null</c> (generic template).</summary>
    public string? DocumentType { get; set; }

    /// <summary>Optimistic-concurrency token.</summary>
    public int Version { get; set; }

    /// <summary>Creation instant (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Last-update instant (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}