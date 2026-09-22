using DMO.Application.Accounts;
using DMO.Application.Authentication;

namespace DMO.Web.Auth;

/// <summary>Result of one login orchestration attempt.</summary>
public abstract record SessionLoginResult
{
    /// <summary>The request shape is invalid (no/ambiguous identifier or missing password). No session.</summary>
    public sealed record InvalidRequest : SessionLoginResult;

    /// <summary>Provider authentication failed with the given reason. No session.</summary>
    /// <param name="Reason">The accepted authentication failure reason (mapped to HTTP by the caller).</param>
    public sealed record AuthenticationFailed(AuthenticationFailureReason Reason) : SessionLoginResult;

    /// <summary>Authentication succeeded but no active application account resolved. No session.</summary>
    public sealed record NoAccess : SessionLoginResult;

    /// <summary>A session was established for the resolved application account.</summary>
    /// <param name="Resolution">The resolved account (<see cref="AccountResolution.Admin"/> or <see cref="AccountResolution.User"/>).</param>
    public sealed record Established(AccountResolution Resolution) : SessionLoginResult;
}

/// <summary>
/// Common login orchestration: authenticate credentials → resolve the application account →
/// establish the session only when allowed.
/// </summary>
/// <remarks>
/// <para>
/// P1-T07 accepted behavior-preserving extraction: the exact orchestration previously inside
/// <c>POST /auth/login</c> lives here once, and every browser-facing login path carries the
/// same semantics:
/// </para>
/// <code>
/// invalid request shape                          → InvalidRequest          (no session)
/// authentication Failed(reason)                  → AuthenticationFailed    (no session)
/// resolution NoAccess                            → NoAccess                (no session)
/// resolution Admin/User                          → Establish session       → Established(resolution)
/// </code>
/// <para>
/// The session is established <b>only</b> when authentication succeeds <b>and</b> resolution
/// returns an active ADMIN/USER (§ P1-T02/P1-T03 posture preserved exactly); every other
/// outcome fails closed. The service performs no HTTP mapping — status/response contracts
/// remain with the endpoint caller — and carries no new authentication model.
/// </para>
/// </remarks>
public sealed class SessionLoginService
{
    private readonly IAuthenticationBoundary _authenticationBoundary;
    private readonly IAccountResolver _accountResolver;
    private readonly ISessionAuthentication _session;

    /// <summary>Creates the service over the accepted authentication/account/session boundary.</summary>
    public SessionLoginService(
        IAuthenticationBoundary authenticationBoundary,
        IAccountResolver accountResolver,
        ISessionAuthentication session)
    {
        ArgumentNullException.ThrowIfNull(authenticationBoundary);
        ArgumentNullException.ThrowIfNull(accountResolver);
        ArgumentNullException.ThrowIfNull(session);
        _authenticationBoundary = authenticationBoundary;
        _accountResolver = accountResolver;
        _session = session;
    }

    /// <summary>Runs the full login orchestration for the presented request.</summary>
    /// <param name="request">The presented request (shape validated here).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<SessionLoginResult> LoginAsync(
        LoginRequest? request,
        CancellationToken cancellationToken)
    {
        var authenticationRequest = ResolveAuthenticationRequest(request);
        if (authenticationRequest is null)
        {
            return new SessionLoginResult.InvalidRequest();
        }

        var outcome = await _authenticationBoundary.AuthenticateAsync(
            authenticationRequest, cancellationToken);

        if (outcome is AuthenticationOutcome.Failed(var failure))
        {
            return new SessionLoginResult.AuthenticationFailed(failure);
        }

        var identity = ((AuthenticationOutcome.Authenticated)outcome).Identity;

        var resolution = await _accountResolver.ResolveAsync(identity, cancellationToken);

        // Fail closed: no session is established when no active application account can be
        // resolved (P1-T02/P1-T03 accepted behavior).
        if (resolution is AccountResolution.NoAccess)
        {
            return new SessionLoginResult.NoAccess();
        }

        await _session.EstablishAsync(identity, cancellationToken);
        return new SessionLoginResult.Established(resolution);
    }

    /// <summary>
    /// Builds the typed authentication request from the presented shape. Exactly one
    /// identifier (email for ADMIN, company number for USER) plus a password is accepted;
    /// both identifiers, neither identifier, or a blank password is invalid.
    /// </summary>
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
            return null;
        }

        return hasEmail
            ? new AdminLoginRequest(body.Email!, body.Password)
            : new UserLoginRequest(body.CompanyNumber!, body.Password);
    }

    /// <summary>
    /// Transport-neutral login request shape.
    /// </summary>
    /// <param name="Email">ADMIN identifier. Never a USER login identifier.</param>
    /// <param name="CompanyNumber">USER canonical login identifier. Never an ADMIN identifier.</param>
    /// <param name="Password">Presented password. Never persisted.</param>
    public sealed record LoginRequest(string? Email, string? CompanyNumber, string? Password);
}