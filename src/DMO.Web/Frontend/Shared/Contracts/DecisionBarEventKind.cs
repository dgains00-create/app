namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// The frozen generic presentation-event vocabulary raised by the decision bar.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §3.4.6. The bar defines no transition result and no domain event: it
/// raises only the generic action invocation with the opaque supplied action key.
/// </remarks>
public enum DecisionBarEventKind
{
    /// <summary>A supplied, enabled action was invoked.</summary>
    ActionInvoked,
}
