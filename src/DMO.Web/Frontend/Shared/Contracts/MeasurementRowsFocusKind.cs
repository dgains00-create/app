namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// The kind of documented focus target produced by a structural change.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §4.2.3. Structure changes never reset focus to page start: every
/// target is either a concrete row entry point or the add control.
/// </remarks>
public enum MeasurementRowsFocusKind
{
    /// <summary>No structural change occurred, so no focus moves.</summary>
    None,

    /// <summary>The first editable control of a newly added row.</summary>
    FirstEditableFieldInRow,

    /// <summary>The first editable control of the nearest surviving row.</summary>
    NearestSurvivingRowFirstEditableField,

    /// <summary>The documented fallback: the add control.</summary>
    AddControl,
}
