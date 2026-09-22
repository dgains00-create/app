namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// A documented focus target produced by a structural change.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §3.3.5 and §4.2.3.
/// <para>
/// The target is a deterministic presentation fact, so the frozen focus rules are unit-testable
/// without a browser. A newly added or surviving row is named by its opaque frontend key; the
/// concrete editable control is resolved after the consumer re-supplies its rows.
/// </para>
/// </remarks>
public sealed record MeasurementRowsFocusTarget
{
    private MeasurementRowsFocusTarget(MeasurementRowsFocusKind kind, string? rowKey, string? fieldKey)
    {
        Kind = kind;
        RowKey = rowKey;
        FieldKey = fieldKey;
    }

    /// <summary>The kind of documented focus target.</summary>
    public MeasurementRowsFocusKind Kind { get; }

    /// <summary>The opaque frontend row key the target belongs to, when applicable.</summary>
    public string? RowKey { get; }

    /// <summary>The opaque frontend field key the target resolves to, when resolved.</summary>
    public string? FieldKey { get; }

    /// <summary>Whether a focus move is documented at all.</summary>
    public bool HasTarget => Kind != MeasurementRowsFocusKind.None;

    /// <summary>Whether the target is the documented add-control fallback.</summary>
    public bool IsAddControl => Kind == MeasurementRowsFocusKind.AddControl;

    /// <summary>The target that moves no focus.</summary>
    public static MeasurementRowsFocusTarget None { get; } =
        new(MeasurementRowsFocusKind.None, null, null);

    /// <summary>The documented fallback target: the add control.</summary>
    public static MeasurementRowsFocusTarget AddControl { get; } =
        new(MeasurementRowsFocusKind.AddControl, null, null);

    /// <summary>The first editable control of the supplied row.</summary>
    /// <exception cref="ArgumentException">The supplied row key is blank.</exception>
    public static MeasurementRowsFocusTarget FirstEditableFieldInRow(string rowKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rowKey);

        return new(MeasurementRowsFocusKind.FirstEditableFieldInRow, rowKey, null);
    }

    /// <summary>The first editable control of the nearest surviving row.</summary>
    /// <exception cref="ArgumentException">The supplied row key is blank.</exception>
    public static MeasurementRowsFocusTarget NearestSurvivingRowFirstEditableField(string rowKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rowKey);

        return new(MeasurementRowsFocusKind.NearestSurvivingRowFirstEditableField, rowKey, null);
    }

    /// <summary>Resolves this target to the supplied concrete editable field key.</summary>
    /// <exception cref="ArgumentException">The supplied field key is blank.</exception>
    public MeasurementRowsFocusTarget WithField(string fieldKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldKey);

        return new(Kind, RowKey, fieldKey);
    }
}
