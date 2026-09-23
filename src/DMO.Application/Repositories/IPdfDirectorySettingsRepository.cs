using DMO.Domain.Controlo;

namespace DMO.Application.Repositories;

/// <summary>
/// The single PDF-directory settings repository contract (P2-T05 contract §20.2, exact).
/// </summary>
/// <remarks>
/// The table is the single-row mechanism (<c>singleton</c> CHECK + UNIQUE): at most one configured
/// base directory exists; an absent row is the explicit <c>not-configured</c> state. P2-T05 owns
/// only the <b>setting</b> (configure/change surface) — generation and availability are P2-T08's
/// (§12.4). The path is never an identity and never printed into a document.
/// </remarks>
public interface IPdfDirectorySettingsRepository
{
    /// <summary>Reads the single configured row, or <c>null</c> (<c>not-configured</c>).</summary>
    Task<PdfDirectorySettings?> GetAsync(CancellationToken cancellationToken);

    /// <summary>Upserts the single row (version = 1 on first set, else version += 1, version-guarded).</summary>
    Task<PdfDirectorySettings> SetAsync(
        PdfDirectorySettings settings,
        CancellationToken cancellationToken);
}