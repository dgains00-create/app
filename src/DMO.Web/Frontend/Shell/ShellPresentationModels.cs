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

public sealed record ProvisionalDestinationPresentation(string DestinationId, string Label);

public sealed record SecondaryDestinationPresentation(
    string Label,
    string Href,
    bool IsCurrent = false);

public sealed record NavigationPresentation(
    IReadOnlyList<PrimaryDestinationPresentation> LiveDestinations,
    IReadOnlyList<ProvisionalDestinationPresentation> ProvisionalDestinations,
    bool AccessResolutionFailed);

public sealed record ShellPresentation(
    IdentityPresentation Identity,
    NavigationPresentation Navigation,
    string PageTitle,
    string PageContext,
    IReadOnlyList<SecondaryDestinationPresentation> SecondaryNavigation,
    string StatusMessage);
