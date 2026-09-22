using DMO.Application.Accounts;
using DMO.Application.Authentication;
using DMO.Application.Session;
using DMO.Web.Auth;

namespace DMO.Web.Endpoints;

/// <summary>
/// Authentication and current-account surface.
/// </summary>
/// <remarks>
/// <para>
/// P1-T03 posture: <c>POST /auth/login</c> performs real authentication (ADMIN through
/// Supabase Auth DEV/TEST; USER through the persistence-backed company_number → carrier
/// email → Supabase password grant), then real account resolution against the persisted
/// mapping (<c>PersistenceAccountLookup</c>). A session is established <b>only</b> when
/// authentication succeeds <b>and</b> resolution returns an active ADMIN/USER; any other
/// outcome fails closed — the surface can never be mistaken for completed login
/// functionality when no mapping or no active account exists.
/// </para>
/// <para>
/// <c>POST /auth/logout</c> only clears the runtime session state. <c>GET /auth/me</c> is
/// read-only and never grants access.
/// </para>
/// </remarks>
public static class AuthEndpoints
{
    /// <summary>Path of the login endpoint.</summary>
    public const string LoginPath = "/auth/login";

    /// <summary>Path of the logout endpoint.</summary>
    public const string LogoutPath = "/auth/logout";

    /// <summary>Path of the current-account endpoint.</summary>
    public const string CurrentAccountPath = "/auth/me";

    /// <summary>Maps the authentication/current-account endpoints onto the application.</summary>
    /// <param name="app">The application to map onto.</param>
    /// <returns>The same application, for chaining.</returns>
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost(LoginPath, async (
            LoginRequest? body,
            IAuthenticationBoundary authenticationBoundary,
            IAccountResolver accountResolver,
            ISessionAuthentication session,
            CancellationToken cancellationToken) =>
        {
            var request = ResolveAuthenticationRequest(body);

            if (request is null)
            {
                return Results.BadRequest();
            }

            var outcome = await authenticationBoundary.AuthenticateAsync(request, cancellationToken);

            if (outcome is AuthenticationOutcome.Failed(var failure))
            {
                return failure switch
                {
                    AuthenticationFailureReason.InvalidCredentials => Results.Unauthorized(),
                    AuthenticationFailureReason.ProviderUnavailable => Results.StatusCode(StatusCodes.Status503ServiceUnavailable),
                    AuthenticationFailureReason.ProviderError => Results.StatusCode(StatusCodes.Status502BadGateway),
                    _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
                };
            }

            var identity = ((AuthenticationOutcome.Authenticated)outcome).Identity;

            var resolution = await accountResolver.ResolveAsync(identity, cancellationToken);

            return resolution switch
            {
                // Fail closed: no session is established when no active application account
                // can be resolved. In P1-T02 production this is always the case.
                AccountResolution.NoAccess _ => Results.StatusCode(StatusCodes.Status403Forbidden),
                _ => await EstablishSessionAndRespondAsync(session, identity, cancellationToken),
            };
        });

        app.MapPost(LogoutPath, async (
            ISessionAuthentication session,
            CancellationToken cancellationToken) =>
        {
            await session.SignOutAsync(cancellationToken);
            return Results.Ok();
        });

        app.MapGet(CurrentAccountPath, async (
            ICurrentAccountContext currentAccountContext,
            CancellationToken cancellationToken) =>
        {
            var current = await currentAccountContext.GetCurrentAsync(cancellationToken);

            return current switch
            {
                CurrentAccount.Admin(var account) => Results.Ok(new MeResponse(
                    AccountType: "admin",
                    DisplayName: account.DisplayName,
                    Email: account.Email,
                    CompanyNumber: null,
                    RoleLabel: null)),
                CurrentAccount.User(var account) => Results.Ok(new MeResponse(
                    AccountType: "user",
                    DisplayName: account.DisplayName,
                    Email: account.Email,
                    CompanyNumber: account.CompanyNumber,
                    RoleLabel: account.RoleLabel)),
                _ => Results.Ok(new MeResponse(
                    AccountType: "none",
                    DisplayName: null,
                    Email: null,
                    CompanyNumber: null,
                    RoleLabel: null)),
            };
        });

        return app;
    }

    private static AuthenticationRequest? ResolveAuthenticationRequest(LoginRequest? body)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Password))
        {
            return null;
        }

        var hasEmail = !string.IsNullOrWhiteSpace(body.Email);
        var hasCompanyNumber = !string.IsNullOrWhiteSpace(body.CompanyNumber);

        if (hasEmail == hasCompanyNumber)
        {
            // Both identifiers present (invalid) or neither (invalid): the transport
            // contract forbids both; no precedence between identifiers is invented.
            return null;
        }

        return hasEmail
            ? new AdminLoginRequest(body.Email!, body.Password)
            : new UserLoginRequest(body.CompanyNumber!, body.Password);
    }

    private static async Task<IResult> EstablishSessionAndRespondAsync(
        ISessionAuthentication session,
        AuthenticatedIdentity identity,
        CancellationToken cancellationToken)
    {
        await session.EstablishAsync(identity, cancellationToken);
        return Results.Ok();
    }

    /// <summary>
    /// Runtime login request shape. Either the ADMIN contract (<c>email</c>) or the USER
    /// contract (<c>companyNumber</c>) is accepted; never both, never neither.
    /// </summary>
    /// <param name="Email">ADMIN login identifier (email + password).</param>
    /// <param name="CompanyNumber">USER login identifier (company_number + password).</param>
    /// <param name="Password">Presented password. Never persisted.</param>
    public sealed record LoginRequest(string? Email, string? CompanyNumber, string? Password);

    /// <summary>Minimal read-only current-account payload. Carries no Template/access facts.</summary>
    /// <param name="AccountType"><c>admin</c>, <c>user</c>, or the state <c>none</c>.</param>
    /// <param name="DisplayName">Application account display name when resolved.</param>
    /// <param name="Email">Account email when resolved (ADMIN uses it as identifier; USER email is independent).</param>
    /// <param name="CompanyNumber">USER company number when resolved.</param>
    /// <param name="RoleLabel">USER presentation-only role label when resolved.</param>
    public sealed record MeResponse(
        string AccountType,
        string? DisplayName,
        string? Email,
        string? CompanyNumber,
        string? RoleLabel);
}