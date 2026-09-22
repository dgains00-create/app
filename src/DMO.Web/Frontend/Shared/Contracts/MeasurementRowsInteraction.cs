namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// The deterministic, presentation-only mechanics model of the repeated-row primitive.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §4.2 (normative semantics) and §3.3.3–§3.3.5.
/// <para>
/// This type is the <b>normative statement</b> of the generic row mechanics; the rows JavaScript
/// asset is a thin DOM adapter over the same rules, so the frozen rules — including the minimum-row
/// invariant and the documented focus targets — are unit-testable without a browser.
/// </para>
/// <para>
/// Frozen semantics:
/// </para>
/// <list type="bullet">
/// <item>row identity is frontend-only and stable: editing never re-keys a row and removal never
/// re-keys the survivors;</item>
/// <item>an accepted removal is applied immediately, so consecutive removals can never take the
/// count below the minimum;</item>
/// <item>removal is refused with the <b>supplied</b> reason, with the pinned precedence structural →
/// minimum → supplied per-row availability;</item>
/// <item>add allocates a new, non-colliding frontend key and appends it;</item>
/// <item>every add/remove outcome documents a focus target; a refused operation documents none and
/// never resets focus to page start.</item>
/// </list>
/// <para>
/// The model holds no service, clock, session, persistence, endpoint or domain rule, and it never
/// validates, converts or computes a supplied value.
/// </para>
/// </remarks>
public sealed class MeasurementRowsInteraction
{
    /// <summary>The default prefix used for frontend-only row keys.</summary>
    public const string DefaultKeyPrefix = "dmo-row-";

    private readonly List<string> _rowKeys = [];
    private readonly Dictionary<string, List<string>> _editableFields = new(StringComparer.Ordinal);
    private int _counter;

    /// <summary>Creates the mechanics model for the supplied minimum.</summary>
    /// <param name="minimumRowCount">The configured minimum; zero means no minimum.</param>
    /// <param name="keyPrefix">The frontend-only prefix used for newly allocated row keys.</param>
    /// <param name="minimumViolationReason">
    /// The supplied reason exposed when removal would violate the minimum. It is mandatory whenever
    /// the minimum is at least one, because removal is then always blocked with a visible reason.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">The minimum is negative.</exception>
    /// <exception cref="ArgumentException">The prefix is blank or a required supplied reason is missing.</exception>
    public MeasurementRowsInteraction(
        int minimumRowCount,
        string keyPrefix = DefaultKeyPrefix,
        string? minimumViolationReason = null)
    {
        if (minimumRowCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumRowCount), minimumRowCount, "The minimum row count cannot be negative.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(keyPrefix);

        if (minimumRowCount >= 1)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(minimumViolationReason);
        }

        MinimumRowCount = minimumRowCount;
        KeyPrefix = keyPrefix;
        MinimumViolationReason = minimumViolationReason;
    }

    /// <summary>The configured minimum row count.</summary>
    public int MinimumRowCount { get; }

    /// <summary>The frontend-only prefix used for newly allocated row keys.</summary>
    public string KeyPrefix { get; }

    /// <summary>The supplied reason exposed when removal would violate the minimum.</summary>
    public string? MinimumViolationReason { get; }

    /// <summary>The tracked row keys, in supplied order. Every key is byte-identical to its supplied value.</summary>
    public IReadOnlyList<string> RowKeys => _rowKeys;

    /// <summary>The tracked row count.</summary>
    public int RowCount => _rowKeys.Count;

    /// <summary>Whether structural mutation (add/remove) is currently allowed.</summary>
    public bool AllowsStructuralMutation { get; private set; } = true;

    /// <summary>The supplied reason exposed while structural mutation is unavailable.</summary>
    public string? StructuralMutationReason { get; private set; }

    /// <summary>Whether per-row removal is currently supplied as available.</summary>
    public bool RowRemovalEnabled { get; private set; } = true;

    /// <summary>The supplied reason exposed while per-row removal is unavailable.</summary>
    public string? RowRemovalDisabledReason { get; private set; }

    /// <summary>The key allocated by the most recent accepted add, or <c>null</c>.</summary>
    public string? LastAllocatedKey { get; private set; }

    /// <summary>The append index of the most recent accepted add, or <c>null</c>.</summary>
    public int? LastAllocatedIndex { get; private set; }

    /// <summary>The documented focus target of the most recent structural change.</summary>
    public MeasurementRowsFocusTarget FocusTarget { get; private set; } = MeasurementRowsFocusTarget.None;

    /// <summary>
    /// The visible reason currently exposed with a disabled remove control, with the pinned
    /// precedence: structural unavailability, then a violated minimum, then supplied availability.
    /// </summary>
    public string? RemovalDisabledReason
    {
        get
        {
            if (!AllowsStructuralMutation)
            {
                return StructuralMutationReason;
            }

            if (RowCount <= MinimumRowCount)
            {
                return MinimumViolationReason;
            }

            return RowRemovalEnabled ? null : RowRemovalDisabledReason;
        }
    }

    /// <summary>
    /// Adopts the consumer's controlled rows: it sets the tracked keys and the supplied
    /// field-editability information and preserves every surviving key exactly. It raises no event
    /// and never re-keys, renumbers or reorders.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// A row key or context is blank, row keys are duplicated, or fewer rows are supplied than the
    /// configured minimum.
    /// </exception>
    public void Sync(IReadOnlyList<MeasurementRowPresentation> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        if (rows.Count < MinimumRowCount)
        {
            throw new ArgumentException(
                "Fewer rows were supplied than the configured minimum.", nameof(rows));
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);

        _rowKeys.Clear();
        _editableFields.Clear();

        foreach (var row in rows)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(row.Key);
            ArgumentException.ThrowIfNullOrWhiteSpace(row.AccessibleContext);

            if (!seen.Add(row.Key))
            {
                throw new ArgumentException("Supplied row keys must be unique.", nameof(rows));
            }

            _rowKeys.Add(row.Key);
            _editableFields[row.Key] = row.Fields
                .Where(field => field.Editable)
                .Select(field => field.Key)
                .ToList();
        }
    }

    /// <summary>
    /// Mirrors the consumer's state-derived structural availability and the associated supplied
    /// reason.
    /// </summary>
    /// <exception cref="ArgumentException">The reason is missing while mutation is disallowed.</exception>
    public void SetStructuralMutation(bool allowed, string? reason)
    {
        if (!allowed)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        }

        AllowsStructuralMutation = allowed;
        StructuralMutationReason = allowed ? null : reason;
    }

    /// <summary>
    /// Mirrors the consumer's supplied per-row remove availability and the associated supplied
    /// reason.
    /// </summary>
    /// <exception cref="ArgumentException">The reason is missing while removal is unavailable.</exception>
    public void SetRowRemovalAvailability(bool enabled, string? reason)
    {
        if (!enabled)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        }

        RowRemovalEnabled = enabled;
        RowRemovalDisabledReason = enabled ? null : reason;
    }

    /// <summary>
    /// Requests one new row: allocates a new, non-colliding frontend-only key, appends it and
    /// documents the new row's first editable control as the focus target.
    /// </summary>
    /// <exception cref="ArgumentException">Structural mutation is disallowed without a supplied reason.</exception>
    public MeasurementRowsOutcome Add()
    {
        if (!AllowsStructuralMutation)
        {
            return MeasurementRowsOutcome.RefusedWith(StructuralMutationReason);
        }

        var index = _rowKeys.Count;
        string key;

        do
        {
            _counter++;
            key = string.Concat(
                KeyPrefix, _counter.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        while (_rowKeys.Contains(key, StringComparer.Ordinal));

        _rowKeys.Add(key);
        _editableFields[key] = [];

        LastAllocatedKey = key;
        LastAllocatedIndex = index;
        FocusTarget = MeasurementRowsFocusTarget.FirstEditableFieldInRow(key);

        return MeasurementRowsOutcome.Accepted(
            MeasurementRowsEvent.AddRequested(key, index), FocusTarget, key, index);
    }

    /// <summary>
    /// Requests the removal of one tracked row. An accepted removal is applied immediately so
    /// consecutive removals can never take the count below the configured minimum.
    /// </summary>
    /// <exception cref="ArgumentException">The supplied row key is blank or is not tracked.</exception>
    public MeasurementRowsOutcome Remove(string rowKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rowKey);

        if (!AllowsStructuralMutation)
        {
            return MeasurementRowsOutcome.RefusedWith(StructuralMutationReason);
        }

        var index = _rowKeys.IndexOf(rowKey);
        if (index < 0)
        {
            throw new ArgumentException("The supplied row key is not tracked.", nameof(rowKey));
        }

        if (RowCount <= MinimumRowCount)
        {
            return MeasurementRowsOutcome.RefusedWith(MinimumViolationReason);
        }

        if (!RowRemovalEnabled)
        {
            return MeasurementRowsOutcome.RefusedWith(RowRemovalDisabledReason);
        }

        _rowKeys.RemoveAt(index);
        _editableFields.Remove(rowKey);

        var nearest = _rowKeys.Count == 0
            ? null
            : index < _rowKeys.Count ? _rowKeys[index] : _rowKeys[^1];

        FocusTarget = nearest is null
            ? MeasurementRowsFocusTarget.AddControl
            : MeasurementRowsFocusTarget.NearestSurvivingRowFirstEditableField(nearest);

        return MeasurementRowsOutcome.Accepted(
            MeasurementRowsEvent.RemoveRequested(rowKey), FocusTarget);
    }

    /// <summary>
    /// Raises the generic value-change hook. It never changes row identity and never validates.
    /// </summary>
    /// <exception cref="ArgumentException">The supplied row key or field key is blank.</exception>
    public MeasurementRowsOutcome ChangeValue(string rowKey, string fieldKey, string? value) =>
        MeasurementRowsOutcome.Accepted(
            MeasurementRowsEvent.ValueChanged(rowKey, fieldKey, value),
            MeasurementRowsFocusTarget.None);

    /// <summary>
    /// Resolves the documented focus intent against the last synced rows: the target row's first
    /// editable field, else the documented add-control fallback. It never returns a page-start
    /// target.
    /// </summary>
    public MeasurementRowsFocusTarget ResolveFocusTarget()
    {
        if (FocusTarget.Kind is MeasurementRowsFocusKind.None or MeasurementRowsFocusKind.AddControl)
        {
            return FocusTarget;
        }

        var rowKey = FocusTarget.RowKey;
        if (rowKey is null)
        {
            return MeasurementRowsFocusTarget.None;
        }

        if (_editableFields.TryGetValue(rowKey, out var fields) && fields.Count > 0)
        {
            return FocusTarget.WithField(fields[0]);
        }

        return MeasurementRowsFocusTarget.AddControl;
    }
}
