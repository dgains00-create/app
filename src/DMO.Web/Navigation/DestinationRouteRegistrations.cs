namespace DMO.Web.Navigation;

/// <summary>
/// P1-T07 documentation seam for future destination-route registration.
/// </summary>
/// <remarks>
/// <para>
/// This class intentionally carries <b>zero data members and zero live registrations</b>.
/// The runtime route lookup authority remains the A-owned
/// <see cref="DMO.Web.Frontend.Shell.IDestinationRouteRegistry"/> seam — currently
/// <see cref="DMO.Web.Frontend.Shell.EmptyDestinationRouteRegistry"/>, registered by
/// <c>AddDmoSharedFrontend</c> and consumed by the A2
/// <see cref="DMO.Web.Frontend.Shell.NavigationProjectionService"/>. P1-T07 must not create a
/// second registry authority or an alternate data source (accepted correction-plan review
/// §11: "Do not create two route registries").
/// </para>
/// <para>
/// Future Workstreams B/C/D/E own their real feature routes: each registers its own
/// <see cref="DMO.Web.Frontend.Shell.IDestinationRouteRegistry"/> implementation (or extends
/// the composition seam through an accepted coordinated change) when its feature surface is
/// implemented, exactly when its Module definition is registered as available in
/// <see cref="DMO.Application.Access.ModuleRegistrations"/>. Nothing is registered in
/// advance by P1-T07 — this build exposes zero operational routes.
/// </para>
/// </remarks>
public static class DestinationRouteRegistrations
{
}