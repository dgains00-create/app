namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// A frozen generic presentation event raised by the repeated-row primitive.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §3.3.7.
/// <para>
/// The event carries only opaque frontend keys and the supplied value. It never carries a canonical
/// identity and the primitive never persists, submits or validates anything.
/// </para>
/// </remarks>
public sealed record MeasurementRowsEvent
{
    private MeasurementRowsEvent(
        MeasurementRowsEventKind kind,
        string? rowKey,
        string? fieldKey,
        string? value,
        int? allocatedIndex)
    {
        Kind = kind;
        RowKey = rowKey;
        FieldKey = fieldKey;
        Value = value;
        AllocatedIndex = allocatedIndex;
    }

    /// <summary>The frozen event kind.</summary>
    public MeasurementRowsEventKind Kind { get; }

    /// <summary>The opaque frontend row key, present for removal and value changes.</summary>
    public string? RowKey { get; }

    /// <summary>The opaque frontend field key, present for a value change.</summary>
    public string? FieldKey { get; }

    /// <summary>The supplied value, present verbatim for a value change.</summary>
    public string? Value { get; }

    /// <summary>The append index of a newly allocated row, present for an add request.</summary>
    public int? AllocatedIndex { get; }

    /// <summary>Creates the add request carrying the newly allocated frontend row key.</summary>
    /// <exception cref="ArgumentException">The allocated row key is blank.</exception>
    public static MeasurementRowsEvent AddRequested(string allocatedRowKey, int allocatedIndex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(allocatedRowKey);

        return new MeasurementRowsEvent(
            MeasurementRowsEventKind.AddRequested, allocatedRowKey, null, null, allocatedIndex);
    }

    /// <summary>Creates the removal request carrying the removed row's opaque frontend key.</summary>
    /// <exception cref="ArgumentException">The supplied row key is blank.</exception>
    public static MeasurementRowsEvent RemoveRequested(string rowKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rowKey);

        return new MeasurementRowsEvent(MeasurementRowsEventKind.RemoveRequested, rowKey, null, null, null);
    }

    /// <summary>Creates the generic value-change hook.</summary>
    /// <exception cref="ArgumentException">The supplied row key or field key is blank.</exception>
    public static MeasurementRowsEvent ValueChanged(string rowKey, string fieldKey, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rowKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldKey);

        return new MeasurementRowsEvent(
            MeasurementRowsEventKind.ValueChanged, rowKey, fieldKey, value ?? string.Empty, null);
    }
}
