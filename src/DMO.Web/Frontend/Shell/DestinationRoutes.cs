namespace DMO.Web.Frontend.Shell;

/// <summary>Presentation-only mapping of a published destination to an implemented route.</summary>
public interface IDestinationRouteRegistry
{
    bool TryGetRoute(string destinationId, out string route);
}

/// <summary>
/// A2 production route registry. It is intentionally empty: no industrial destination is
/// implemented yet, and A must not invent B/C/D/E routes.
/// </summary>
public sealed class EmptyDestinationRouteRegistry : IDestinationRouteRegistry
{
    public bool TryGetRoute(string destinationId, out string route)
    {
        route = string.Empty;
        return false;
    }
}
