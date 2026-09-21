namespace DMO.Application.Authentication;

/// <summary>
/// Boundary between the concrete credential/provider flow and the application.
/// </summary>
/// <remarks>
/// The provider supplies <see cref="AuthenticatedIdentity"/> only. The boundary never resolves
/// application accounts and never decides access.
/// </remarks>
public interface IAuthenticationBoundary
{
    /// <summary>
    /// Attempts to authenticate the presented request through the configured provider flow.
    /// </summary>
    /// <param name="request">Typed login request (ADMIN or USER contract).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <see cref="AuthenticationOutcome.Authenticated"/> with the minimal identity, or
    /// <see cref="AuthenticationOutcome.Failed"/> with a provider/credential reason.
    /// </returns>
    Task<AuthenticationOutcome> AuthenticateAsync(
        AuthenticationRequest request,
        CancellationToken cancellationToken);
}