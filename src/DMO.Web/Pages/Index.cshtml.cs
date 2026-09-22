using DMO.Application.Session;
using DMO.Web.Navigation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DMO.Web.Pages;

/// <summary>
/// P1-T07 account-aware root router. <c>GET /</c> never renders a page; every path is an
/// exact redirect.
/// </summary>
/// <remarks>
/// <para>
/// Exact dispatch (accepted P1-T07 contract, request §13):
/// </para>
/// <code>
/// CurrentAccount.None (no session / unresolvable session)   → 302 → /Login
/// CurrentAccount.Admin (active ADMIN)                       → 302 → /Administration
/// CurrentAccount.User(user):
///     UserLandingService.ResolveAsync(user)                  // single A2 projection
///     RedirectTo(route)                                      → 302 → route (real registered route)
///     NoAccess                                               → 302 → /AccessDenied (fail closed)
/// </code>
/// <para>
/// No role strings, no <c>Template.Name</c> logic, no provider claims, no debug/bootstrap
/// data. Account classification comes exclusively from the accepted
/// <see cref="DMO.Application.Session.ICurrentAccountContext"/>; USER landing behavior comes
/// exclusively from <see cref="UserLandingService"/> over the accepted A2 projection.
/// No <c>ReturnUrl</c> mechanism exists (no open-redirect surface).
/// </para>
/// </remarks>
public sealed class IndexModel : PageModel
{
    private readonly ICurrentAccountContext _currentAccount;
    private readonly UserLandingService _userLanding;

    /// <summary>Creates the root router over the current-account boundary and the USER landing service.</summary>
    public IndexModel(ICurrentAccountContext currentAccount, UserLandingService userLanding)
    {
        ArgumentNullException.ThrowIfNull(currentAccount);
        ArgumentNullException.ThrowIfNull(userLanding);
        _currentAccount = currentAccount;
        _userLanding = userLanding;
    }

    /// <inheritdoc />
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var current = await _currentAccount.GetCurrentAsync(cancellationToken);

        switch (current)
        {
            // No session (or the session identity failed to resolve to an active account):
            // fail closed at the public boundary.
            case CurrentAccount.None:
                return RedirectToPage("/Login");

            // The dedicated ADMIN has no operational Template and no USER navigation; it
            // lands on the ADMIN-only administration surface.
            case CurrentAccount.Admin:
                return RedirectToPage("/Administration/Index");

            case CurrentAccount.User(var user):
                var landing = await _userLanding.ResolveAsync(
                    new CurrentAccount.User(user), cancellationToken);
                return landing switch
                {
                    UserLanding.RedirectTo(var route) => Redirect(route),
                    _ => RedirectToPage("/AccessDenied"),
                };

            default:
                return RedirectToPage("/Login");
        }
    }
}