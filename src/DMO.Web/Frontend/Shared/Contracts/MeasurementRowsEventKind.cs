namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// The frozen generic presentation-event vocabulary raised by the repeated-row primitive.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §3.3.7. The primitive performs no validation, no calculation and no
/// persistence, so no event carries a verdict or a domain result.
/// </remarks>
public enum MeasurementRowsEventKind
{
    /// <summary>The consumer is asked to add a row carrying the newly allocated frontend key.</summary>
    AddRequested,

    /// <summary>The consumer is asked to remove the row carrying the supplied opaque key.</summary>
    RemoveRequested,

    /// <summary>A supplied field value changed; the generic binding hook only.</summary>
    ValueChanged,
}
