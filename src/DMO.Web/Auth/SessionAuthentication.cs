using System.Security.Claims;
using DMO.Application.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace DMO.Web.Auth;

/// <summary>
/// Runtime session element: establishes and clears the authenticated session, and exposes
/// the current authenticated identity to account resolution.
/// </summary>
/// <remarks>
/// <para>
/// Session mutation is a runtime concern in <c>DMO.Web</c>; application contracts never
/// expose it. The session principal carries exactly two claims — provider subject and
/// authentication path — which are identity linkage only and never access claims.
/// </para>
/// <para>
/// The concrete session mechanism is the ASP.NET Core cookie authentication scheme. In
/// P1-T02 production no session can be established, because account resolution always fails
/// closed (§ P1-T02 V5); the seams are real and exercised by tests with test-only fakes.
/// </para>
/// </remarks>
public interface ISessionAuthentication
{
    /// <summary>
    /// Returns the authenticated identity of the current session, or <c>null</c> when there
    /// is no valid session.
    /// </summary>
    Task<AuthenticatedIdentity?> GetIdentityAsync(CancellationToken cancellationToken);

    /// <summary>Establishes a session for the authenticated identity.</summary>
    Task EstablishAsync(AuthenticatedIdentity identity, CancellationToken cancellationToken);

    /// <summary>Clears the current session state (sign-out).</summary>
    Task SignOutAsync(CancellationToken cancellationToken);
}

/// <summary>
/// ASP.NET Core cookie-scheme implementation of <see cref="ISessionAuthentication"/>.
/// </summary>
public sealed class SessionAuthentication : ISessionAuthentication
{
    /// <summary>Session claim holding the provider subject (internal linkage only).</summary>
    public const string ProviderSubjectClaim = "dmo:provider_subject";

    /// <summary>Session claim holding the authentication path discriminator.</summary>
    public const string AuthenticationPathClaim = "dmo:authentication_path";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SessionAuthentication> _logger;

    /// <summary>Creates the session element over the current HTTP context.</summary>
    public SessionAuthentication(
        IHttpContextAccessor httpContextAccessor,
        ILogger<SessionAuthentication> logger)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        ArgumentNullException.ThrowIfNull(logger);
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AuthenticatedIdentity?> GetIdentityAsync(CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return null;
        }

        var result = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!result.Succeeded || result.Principal is null)
        {
            return null;
        }

        var subject = result.Principal.FindFirstValue(ProviderSubjectClaim);
        var pathValue = result.Principal.FindFirstValue(AuthenticationPathClaim);

        if (string.IsNullOrWhiteSpace(subject)
            || !Enum.TryParse<AuthenticationPath>(pathValue, out var authenticationPath))
        {
            // A session without the exact accepted claims is not a valid identity.
            return null;
        }

        return new AuthenticatedIdentity(subject, authenticationPath);
    }

    /// <inheritdoc />
    public async Task EstablishAsync(AuthenticatedIdentity identity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identity);

        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException(
                "No active HTTP context: a session can only be established within a request.");

        var claims = new[]
        {
            new Claim(ProviderSubjectClaim, identity.ProviderSubject),
            new Claim(AuthenticationPathClaim, identity.AuthenticationPath.ToString()),
        };

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        _logger.LogInformation(
            "Session established (authentication path {AuthenticationPath}).",
            identity.AuthenticationPath);
    }

    /// <inheritdoc />
    public async Task SignOutAsync(CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("Session cleared.");
    }
}