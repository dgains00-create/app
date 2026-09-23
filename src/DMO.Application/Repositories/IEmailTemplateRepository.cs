using DMO.Domain.Controlo;

namespace DMO.Application.Repositories;

/// <summary>
/// The single email-template repository contract (P2-T05 contract §20.2, exact).
/// </summary>
/// <remarks>
/// Templates store plain text verbatim — no placeholder parsing, substitution or validation exists
/// in P2-T05 (Q-PLACE). The unique-name violation (<c>email_templates_name_key</c>) maps to the
/// <c>duplicate-name</c> refusal; a delete blocked by a future dependent maps to
/// <c>dependency-exists</c> (RESTRICT backstop).
/// </remarks>
public interface IEmailTemplateRepository
{
    /// <summary>Reads one template, or <c>null</c>.</summary>
    Task<EmailTemplate?> GetByIdAsync(Guid emailTemplateId, CancellationToken cancellationToken);

    /// <summary>Lists every template in deterministic name order.</summary>
    Task<IReadOnlyList<EmailTemplate>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Inserts the template (version = 1).</summary>
    Task<EmailTemplate> CreatedAsync(EmailTemplate template, CancellationToken cancellationToken);

    /// <summary>Updates the template (version += 1, version-guarded).</summary>
    Task<EmailTemplate> UpdatedAsync(EmailTemplate template, CancellationToken cancellationToken);

    /// <summary>Deletes the template (version-guarded; RESTRICT backstop).</summary>
    Task DeletedAsync(Guid emailTemplateId, int expectedVersion, CancellationToken cancellationToken);
}