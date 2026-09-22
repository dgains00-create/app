namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// One supplied repeated row.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §3.3.2 and §3.3.3 (P2-T03 PIN).
/// <para>
/// <see cref="Key"/> is a <b>frontend-only</b> presentation identity: it is never persisted, never
/// submitted, never sent to a backend and never a canonical measurement or database identity. It is
/// stable across edit and re-render, and removing a row never re-keys the surviving rows.
/// </para>
/// <para>
/// <see cref="AccessibleContext"/> is supplied (never derived from a field value) and disambiguates
/// the row's controls for assistive technology.
/// </para>
/// </remarks>
public sealed record MeasurementRowPresentation
{
    private MeasurementRowPresentation(
        string key,
        string accessibleContext,
        IReadOnlyList<MeasurementRowFieldPresentation> fields,
        string? validationText,
        StatusTone validationTone)
    {
        Key = key;
        AccessibleContext = accessibleContext;
        Fields = fields;
        ValidationText = validationText;
        ValidationTone = validationTone;
    }

    /// <summary>Opaque frontend row key. Never a canonical identity.</summary>
    public string Key { get; }

    /// <summary>The supplied row context used to disambiguate the row's controls.</summary>
    public string AccessibleContext { get; }

    /// <summary>The supplied fields, in exactly the supplied order.</summary>
    public IReadOnlyList<MeasurementRowFieldPresentation> Fields { get; }

    /// <summary>Row-level consumer-supplied validation display text, rendered verbatim.</summary>
    public string? ValidationText { get; }

    /// <summary>Supplementary row validation tone; the text is always present, never colour-only.</summary>
    public StatusTone ValidationTone { get; }

    /// <summary>Whether supplied row-level validation display text accompanies the row.</summary>
    public bool HasValidationText => !string.IsNullOrWhiteSpace(ValidationText);

    /// <summary>The stable tone token of the supplied row validation display.</summary>
    public string ValidationToneToken => RecordStatusPresentation.Normalize(ValidationTone) switch
    {
        StatusTone.Info => "info",
        StatusTone.Success => "success",
        StatusTone.Warning => "warning",
        StatusTone.Danger => "danger",
        _ => "neutral",
    };

    /// <summary>Whether any supplied field is editable.</summary>
    public bool HasEditableField => Fields.Any(rowField => rowField.Editable);

    /// <summary>The first editable supplied field key, or <c>null</c> when the row has none.</summary>
    public string? FirstEditableFieldKey =>
        Fields.FirstOrDefault(rowField => rowField.Editable)?.Key;

    /// <summary>Creates a repeated row, failing closed on a supplied inconsistency.</summary>
    /// <exception cref="ArgumentException">
    /// The row key or accessible context is blank; a field key is blank; field keys are duplicated.
    /// </exception>
    public static MeasurementRowPresentation Create(
        string key,
        string accessibleContext,
        IReadOnlyList<MeasurementRowFieldPresentation>? fields = null,
        string? validationText = null,
        StatusTone validationTone = StatusTone.Neutral)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessibleContext);

        var suppliedFields = fields ?? [];
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var field in suppliedFields)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(field.Key);

            if (!seen.Add(field.Key))
            {
                throw new ArgumentException(
                    "Supplied field keys must be unique within a row.", nameof(fields));
            }
        }

        return new MeasurementRowPresentation(
            key,
            accessibleContext,
            suppliedFields,
            validationText,
            RecordStatusPresentation.Normalize(validationTone));
    }
}
