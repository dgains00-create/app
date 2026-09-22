namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// One supplied field of a repeated row.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §3.3.2 (P2-T03 PIN).
/// <para>
/// The consumer owns the field entirely: its opaque key, its visible label, its controlled value,
/// its generic control kind, its options, its editability and its validation <b>display</b>. The
/// primitive defines no field list, no field naming, no formula, no tolerance, no nominal value and
/// no validation rule, and it never computes a verdict from a supplied value.
/// </para>
/// </remarks>
public sealed record MeasurementRowFieldPresentation
{
    private MeasurementRowFieldPresentation(
        string key,
        string label,
        string value,
        MeasurementRowFieldKind kind,
        IReadOnlyList<MeasurementRowFieldOptionPresentation> options,
        bool editable,
        string? disabledReason,
        string? validationText,
        StatusTone validationTone)
    {
        Key = key;
        Label = label;
        Value = value;
        Kind = kind;
        Options = options;
        Editable = editable;
        DisabledReason = disabledReason;
        ValidationText = validationText;
        ValidationTone = validationTone;
    }

    /// <summary>Opaque frontend field key, unique within its row. Never a canonical identity.</summary>
    public string Key { get; }

    /// <summary>The supplied visible label (the consumer owns all field naming).</summary>
    public string Label { get; }

    /// <summary>The controlled supplied value, rendered verbatim. May be empty; never validated.</summary>
    public string Value { get; }

    /// <summary>The generic control kind.</summary>
    public MeasurementRowFieldKind Kind { get; }

    /// <summary>The supplied options, presented only for a choice field.</summary>
    public IReadOnlyList<MeasurementRowFieldOptionPresentation> Options { get; }

    /// <summary>Whether the supplied field is editable.</summary>
    public bool Editable { get; }

    /// <summary>The supplied reason exposed when the field is not editable.</summary>
    public string? DisabledReason { get; }

    /// <summary>Consumer-supplied validation <b>display</b> text, rendered verbatim.</summary>
    public string? ValidationText { get; }

    /// <summary>Supplementary validation tone; the text is always present, never colour-only.</summary>
    public StatusTone ValidationTone { get; }

    /// <summary>Whether the supplied field is a choice field with options.</summary>
    public bool HasOptions => Options.Count > 0;

    /// <summary>Whether the field is read-only.</summary>
    public bool IsReadOnly => !Editable;

    /// <summary>Whether supplied validation display text accompanies the field.</summary>
    public bool HasValidationText => !string.IsNullOrWhiteSpace(ValidationText);

    /// <summary>The stable tone token of the supplied validation display.</summary>
    public string ValidationToneToken => RecordStatusPresentation.Normalize(ValidationTone) switch
    {
        StatusTone.Info => "info",
        StatusTone.Success => "success",
        StatusTone.Warning => "warning",
        StatusTone.Danger => "danger",
        _ => "neutral",
    };

    /// <summary>The stable CSS token for the generic control kind.</summary>
    public string KindToken => Kind switch
    {
        MeasurementRowFieldKind.Numeric => "numeric",
        MeasurementRowFieldKind.Choice => "choice",
        _ => "text",
    };

    /// <summary>Creates a row field, failing closed on a supplied inconsistency.</summary>
    /// <exception cref="ArgumentException">
    /// The key or label is blank; the key is missing; a choice field has no options; a non-choice
    /// field is supplied with options; option values are duplicated or blank; a read-only field
    /// carries no supplied reason.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">The supplied kind is not a defined value.</exception>
    public static MeasurementRowFieldPresentation Create(
        string key,
        string label,
        string? value = null,
        MeasurementRowFieldKind kind = MeasurementRowFieldKind.Text,
        IReadOnlyList<MeasurementRowFieldOptionPresentation>? options = null,
        bool editable = true,
        string? disabledReason = null,
        string? validationText = null,
        StatusTone validationTone = StatusTone.Neutral)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown field kind.");
        }

        var suppliedOptions = options ?? [];

        if (kind == MeasurementRowFieldKind.Choice)
        {
            if (suppliedOptions.Count == 0)
            {
                throw new ArgumentException(
                    "A choice field requires supplied options.", nameof(options));
            }
        }
        else if (suppliedOptions.Count > 0)
        {
            throw new ArgumentException(
                "Options are supplied only for a choice field.", nameof(options));
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var option in suppliedOptions)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(option.Value);
            ArgumentException.ThrowIfNullOrWhiteSpace(option.Label);

            if (!seen.Add(option.Value))
            {
                throw new ArgumentException(
                    "Supplied option values must be unique.", nameof(options));
            }
        }

        if (!editable)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(disabledReason);
        }

        return new MeasurementRowFieldPresentation(
            key,
            label,
            value ?? string.Empty,
            kind,
            suppliedOptions,
            editable,
            disabledReason,
            validationText,
            RecordStatusPresentation.Normalize(validationTone));
    }
}
