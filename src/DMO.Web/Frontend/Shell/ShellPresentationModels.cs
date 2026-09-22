using DMO.Application.Access;

namespace DMO.Web.Frontend.Shell;

public sealed record IdentityPresentation(
    string DisplayName,
    string AccountKind,
    string? SecondaryIdentifier,
    string? RoleLabel);

public sealed record PrimaryDestinationPresentation(
    string DestinationId,
    string Label,
    string Href,
    IReadOnlyList<ModuleId> GrantedModuleIds,
    bool IsCurrent = false);

public sealed record SecondaryDestinationPresentation(
    string Label,
    string Href,
    bool IsCurrent = false);

/// <summary>
/// Navigation state of the shared shell: the live destinations actually available in this
/// build plus whether operational access resolution failed closed.
/// </summary>
/// <param name="LiveDestinations">
/// The live destinations derived from effective granted Modules ∩ available definitions ∩
/// non-contextual Modules ∩ actually registered routes, in Template presentation order.
/// </param>
/// <param name="AccessResolutionFailed">
/// Whether operational access resolution failed closed (denied resolution). When
/// <c>true</c> the live list is empty by contract.
/// </param>
/// <param name="LandingDestinationId">
/// The nullable persisted landing destination id carried from the accepted access outcome.
/// This is a routing fact propagated for the P1-T07 landing selector; it is not permission
/// authority and does not change projection behavior.
/// </param>
public sealed record NavigationPresentation(
    IReadOnlyList<PrimaryDestinationPresentation> LiveDestinations,
    bool AccessResolutionFailed,
    string? LandingDestinationId);

public sealed record ShellPresentation(
    IdentityPresentation Identity,
    NavigationPresentation Navigation,
    string PageTitle,
    string PageContext,
    IReadOnlyList<SecondaryDestinationPresentation> SecondaryNavigation,
    string StatusMessage);
