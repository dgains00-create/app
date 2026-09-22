namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// The generic control chosen for a supplied row field.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §3.3.2.
/// <para>
/// The kind selects a native control affordance only. It carries <b>no</b> numeric, precision,
/// rounding, unit, range, step, minimum/maximum or scale semantics: the primitive never parses,
/// validates or computes a supplied value.
/// </para>
/// </remarks>
public enum MeasurementRowFieldKind
{
    /// <summary>A single-line text affordance.</summary>
    Text,

    /// <summary>A native numeric affordance, with no numeric rule attached.</summary>
    Numeric,

    /// <summary>A supplied-option affordance.</summary>
    Choice,
}
