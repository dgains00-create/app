using DMO.Application.Accounts;
using DMO.Application.Session;
using DMO.Web.Auth;
using DMO.Web.Navigation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DMO.Web.Pages;

/// <summary>
/// P1-T07 public login page. Minimal public surface; the operational A2 shell is never
/// asked to render unauthenticated users.
/// </summary>
/// <remarks>
/// <para>
/// The page delegates the complete login orchestration to
/// <see cref="SessionLoginService"/> (the same behavior-preserving flow used by
/// <c>POST /auth/login</c>): authenticate credentials → resolve application account →
/// establish the session only when allowed. It performs no authentication of its own and
/// adds no new auth model.
/// </para>
/// <para>
/// Post-login dispatch mirrors the accepted root contract:
/// </para>
/// <code>
/// ADMIN  → /Administration
/// USER   → UserLandingService.ResolveAsync → real landing route, or /AccessDenied
/// </code>
/// <para>
/// All non-success login outcomes are intentionally indistinguishable at the UI: one generic
/// message, no provider/account reason mapping, no session. No <c>ReturnUrl</c> mechanism
/// exists.
/// </para>
/// </remarks>
public sealed class LoginModel : PageModel
{
    /// <summary>Single generic user-facing message for every unsuccessful attempt.</summary>
    public const string GenericErrorMessage =
        "A autenticação falhou. Verifique os dados introduzidos e tente novamente.";

    private readonly SessionLoginService _login;
    private readonly UserLandingService _userLanding;

    /// <summary>Creates the page over the shared login orchestration and the USER landing service.</summary>
    public LoginModel(SessionLoginService login, UserLandingService userLanding)
    {
        ArgumentNullException.ThrowIfNull(login);
        ArgumentNullException.ThrowIfNull(userLanding);
        _login = login;
        _userLanding = userLanding;
    }

    /// <summary>ADMIN identifier (email). Never a USER login identifier.</summary>
    [BindProperty]
    public string? Email { get; set; }

    /// <summary>USER canonical login identifier. Never an ADMIN identifier.</summary>
    [BindProperty]
    public string? CompanyNumber { get; set; }

    /// <summary>Presented password. Never persisted.</summary>
    [BindProperty]
    public string? Password { get; set; }

    /// <summary>Generic login failure message, or <c>null</c> on first render.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Renders the public login form.</summary>
    public void OnGet()
    {
    }

    /// <summary>Runs the shared login orchestration and dispatches the established account.</summary>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var result = await _login.LoginAsync(
            new SessionLoginService.LoginRequest(Email, CompanyNumber, Password),
            cancellationToken);

        if (result is not SessionLoginResult.Established(var resolution))
        {
            // Intentionally indistinguishable failure: one generic message for invalid
            // shape, provider failures and unresolved accounts; no session was established.
            ErrorMessage = GenericErrorMessage;
            return Page();
        }

        return resolution switch
        {
            AccountResolution.Admin _ => RedirectToPage("/Administration/Index"),
            AccountResolution.User(var user) => await ResolveUserLandingAsync(user, cancellationToken),
            _ => RedirectToPage("/Login"),
        };
    }

    private async Task<IActionResult> ResolveUserLandingAsync(
        UserAccount user,
        CancellationToken cancellationToken)
    {
        var landing = await _userLanding.ResolveAsync(
            new CurrentAccount.User(user), cancellationToken);

        return landing switch
        {
            UserLanding.RedirectTo(var route) => Redirect(route),
            _ => RedirectToPage("/AccessDenied"),
        };
    }
}