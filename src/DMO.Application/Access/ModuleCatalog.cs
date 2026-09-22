namespace DMO.Application.Access;

/// <summary>
/// One entry of the canonical Module vocabulary: stable identity plus Master presentation
/// metadata.
/// </summary>
/// <remarks>
/// The vocabulary is exactly the 13 assignable Modules of <c>ACCESS_MODEL</c> §1, in the
/// Master canonical order. <see cref="Id"/> is the authority; <see cref="DisplayName"/> and
/// <see cref="DestinationId"/> are presentation/navigation metadata and never carry
/// authorization meaning.
/// </remarks>
/// <param name="Id">Stable code-owned Module identity.</param>
/// <param name="DisplayName">Master presentation name (accents/spaces allowed).</param>
/// <param name="DestinationId">
/// Stable visible-destination identifier, or <c>null</c> for contextual-only Modules
/// (Ferramentas / Ferramentas Approve).
/// </param>
public sealed record ModuleVocabularyEntry(
    ModuleId Id,
    string DisplayName,
    string? DestinationId);

/// <summary>
/// Canonical, code-owned Module identity vocabulary — the single source of the 13 assignable
/// Modules and their Master presentation metadata.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ModuleCatalog"/> answers <i>what stable Module identities exist in the product
/// contract</i>. It deliberately says nothing about current-build availability: a known
/// identity becomes selectable/enforceable only when a real surface is registered through
/// <see cref="ModuleRegistrations"/> (ACCESS_MODEL §6: "A planned but not-yet-implemented
/// module is not a usable Template choice until its real surface is registered as
/// available").
/// </para>
/// <para>
/// The catalog is plain auditable code — no reflection, no plugin/assembly scanning, no
/// database definition table. New assignable Modules are introduced by development, never by
/// Admin.
/// </para>
/// </remarks>
public static class ModuleCatalog
{
    /// <summary>Job On View — read/consultation surface of the Job On destination.</summary>
    public static readonly ModuleId JobOnView = ModuleId.From("job-on-view");

    /// <summary>Job On Create — create/edit/manage surface of the Job On destination.</summary>
    public static readonly ModuleId JobOnCreate = ModuleId.From("job-on-create");

    /// <summary>Controlo Create — creation/measurement/submission surface.</summary>
    public static readonly ModuleId ControloCreate = ModuleId.From("controlo-create");

    /// <summary>Controlo Approve — review/approval/decision surface.</summary>
    public static readonly ModuleId ControloApprove = ModuleId.From("controlo-approve");

    /// <summary>Reparação Interna — top-level repair destination.</summary>
    public static readonly ModuleId ReparacaoInterna = ModuleId.From("reparacao-interna");

    /// <summary>Boquilhas — top-level Module.</summary>
    public static readonly ModuleId Boquilhas = ModuleId.From("boquilhas");

    /// <summary>Armazém — top-level Module.</summary>
    public static readonly ModuleId Armazem = ModuleId.From("armazem");

    /// <summary>Reparação Programada View — read surface of the shared destination.</summary>
    public static readonly ModuleId ReparacaoProgramadaView = ModuleId.From("reparacao-programada-view");

    /// <summary>Reparação Programada Create — management surface of the shared destination.</summary>
    public static readonly ModuleId ReparacaoProgramadaCreate = ModuleId.From("reparacao-programada-create");

    /// <summary>Tampões — top-level Module.</summary>
    public static readonly ModuleId Tampoes = ModuleId.From("tampoes");

    /// <summary>História — top-level Module.</summary>
    public static readonly ModuleId Historia = ModuleId.From("historia");

    /// <summary>Ferramentas — contextual-only access unit, no top-level destination.</summary>
    public static readonly ModuleId Ferramentas = ModuleId.From("ferramentas");

    /// <summary>Ferramentas Approve — contextual-only access unit, no top-level destination.</summary>
    public static readonly ModuleId FerramentasApprove = ModuleId.From("ferramentas-approve");

    /// <summary>
    /// The 13 canonical Modules in the Master order (ACCESS_MODEL §1).
    /// </summary>
    public static IReadOnlyList<ModuleVocabularyEntry> All { get; } =
    [
        new ModuleVocabularyEntry(JobOnView, "Job On View", "job-on"),
        new ModuleVocabularyEntry(JobOnCreate, "Job On Create", "job-on"),
        new ModuleVocabularyEntry(ControloCreate, "Controlo Create", "controlo"),
        new ModuleVocabularyEntry(ControloApprove, "Controlo Approve", "controlo"),
        new ModuleVocabularyEntry(ReparacaoInterna, "Reparação Interna", "reparacao-interna"),
        new ModuleVocabularyEntry(Boquilhas, "Boquilhas", "boquilhas"),
        new ModuleVocabularyEntry(Armazem, "Armazém", "armazem"),
        new ModuleVocabularyEntry(ReparacaoProgramadaView, "Reparação Programada View", "reparacao-programada"),
        new ModuleVocabularyEntry(ReparacaoProgramadaCreate, "Reparação Programada Create", "reparacao-programada"),
        new ModuleVocabularyEntry(Tampoes, "Tampões", "tampoes"),
        new ModuleVocabularyEntry(Historia, "História", "historia"),
        new ModuleVocabularyEntry(Ferramentas, "Ferramentas", null),
        new ModuleVocabularyEntry(FerramentasApprove, "Ferramentas Approve", null),
    ];

    /// <summary>
    /// Returns whether the given identity belongs to the canonical vocabulary.
    /// </summary>
    public static bool IsKnown(ModuleId id) => KnownValues.Contains(id);

    /// <summary>
    /// Finds the canonical identity for a persisted value, or <c>null</c> when the value is
    /// not a valid/known Module identity.
    /// </summary>
    public static ModuleId? FindByValue(string value) =>
        ModuleId.TryParse(value, out var id) && IsKnown(id) ? id : null;

    private static readonly HashSet<ModuleId> KnownValues = new(All.Select(entry => entry.Id));
}