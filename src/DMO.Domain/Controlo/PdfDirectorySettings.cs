namespace DMO.Domain.Controlo;

/// <summary>
/// The single-row PDF/document base-directory setting.
/// </summary>
/// <remarks>
/// Authority: P2-T05 contract §12.2 and §16.5. <see cref="BaseDirectory"/> is an absolute
/// <b>server-host</b> filesystem path (Q-PDF ruling: server-host filesystem configuration with a
/// server-side accessibility check), stored verbatim (trimmed). An absent row is the explicit
/// <c>not-configured</c> state, never an error. The path is never an identity, never a join key and
/// is never printed into a document (P2-T08 owns generation).
/// </remarks>
public sealed record PdfDirectorySettings(
    Guid PdfDirectorySettingId,
    string BaseDirectory,
    int Version,
    DateTimeOffset UpdatedAt);