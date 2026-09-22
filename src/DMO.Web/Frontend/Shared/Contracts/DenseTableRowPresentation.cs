namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// A supplied row of a <c>DenseDataTable</c>.
/// </summary>
/// <remarks>
/// Authority: P2-T02 contract §5.3, §5.4.
/// <para>
/// <see cref="Key"/> is the only stable identity used for selection and event carriers, and
/// is <b>opaque</b>: never a canonical identity and never used to build a URL.
/// <see cref="AccessibleContext"/> is supplied human context used to disambiguate the row for
/// assistive technology and to qualify row-action accessible names; the component never
/// derives it from cell display text (A1 freeze §3 rule 4).
/// </para>
/// <para>
/// Row actions are row-scoped only and reuse the accepted P2-T01
/// <see cref="SharedActionPresentation"/>, which already enforces the disabled-reason rule.
/// No per-row selectability or per-row open flag is invented: selection and open are
/// table-level consumer decisions.
/// </para>
/// </remarks>
public sealed record DenseTableRowPresentation
{
    private DenseTableRowPresentation(
        string key,
        string accessibleContext,
        IReadOnlyList<DenseTableCellPresentation> cells,
        RecordStatusPresentation? status,
        IReadOnlyList<SharedActionPresentation> actions)
    {
        Key = key;
        AccessibleContext = accessibleContext;
        Cells = cells;
        Status = status;
        Actions = actions;
    }

    /// <summary>Opaque, stable row identity across re-render. Never a canonical identity or URL.</summary>
    public string Key { get; }

    /// <summary>
    /// Mandatory supplied human row context, used for assistive disambiguation and to qualify
    /// row-action accessible names. Never derived from cell display text.
    /// </summary>
    public string AccessibleContext { get; }

    /// <summary>
    /// Mandatory supplied cells. The count must equal the supplied column count; the
    /// table rejects a mismatch before rendering.
    /// </summary>
    public IReadOnlyList<DenseTableCellPresentation> Cells { get; }

    /// <summary>Optional supplied row status, rendered by the accepted P2-T01 status partial.</summary>
    public RecordStatusPresentation? Status { get; }

    /// <summary>Optional row-scoped generic actions (accepted P2-T01 carrier). Empty by default.</summary>
    public IReadOnlyList<SharedActionPresentation> Actions { get; }

    /// <summary>The supplied row status, or <c>null</c>. A row without a status renders none.</summary>
    public RecordStatusPresentation? VisibleStatus => Status;

    /// <summary>Whether a supplied row status accompanies the row.</summary>
    public bool HasStatus => Status is not null;

    /// <summary>The supplied row actions that are presented at all.</summary>
    public IReadOnlyList<SharedActionPresentation> VisibleActions =>
        Actions.Where(action => action.Visible).ToList();

    /// <summary>Whether at least one supplied row action is presented.</summary>
    public bool HasActions => VisibleActions.Count > 0;

    /// <summary>
    /// Whether the row carries no supplied presented action and no open control exists for it,
    /// so no interactive element is rendered inside the row.
    /// </summary>
    public bool IsReadOnly(bool openEnabled) => !HasActions && !openEnabled;

    /// <summary>Creates a table row.</summary>
    /// <exception cref="ArgumentException">The supplied key or accessible context is blank.</exception>
    /// <remarks>
    /// The cell count is validated against the supplied column count by
    /// <see cref="DenseTablePresentation.Create"/>, which is the only place where both are known.
    /// </remarks>
    public static DenseTableRowPresentation Create(
        string key,
        string accessibleContext,
        IReadOnlyList<DenseTableCellPresentation> cells,
        RecordStatusPresentation? status = null,
        IReadOnlyList<SharedActionPresentation>? actions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessibleContext);
        ArgumentNullException.ThrowIfNull(cells);

        return new DenseTableRowPresentation(key, accessibleContext, cells, status, actions ?? []);
    }
}
