namespace DMO.Application.Authentication;

/// <summary>
/// Result of an authentication attempt.
/// </summary>
/// <remarks>
/// Authentication answers only: <i>did the credential/provider operation authenticate an
/// identity?</i> It says nothing about application accounts or access — those are account
/// resolution concerns.
/// </remarks>
public abstract record AuthenticationOutcome
{
    private AuthenticationOutcome()
    {
    }

    /// <summary>
    /// An identity was established. Nothing else — no account, no access, no token.
    /// </summary>
    /// <param name="Identity">The minimal authenticated identity.</param>
    public sealed record Authenticated(AuthenticatedIdentity Identity) : AuthenticationOutcome;

    /// <summary>
    /// The attempt failed for a provider/credential fact.
    /// </summary>
    /// <param name="Reason">The provider/credential reason.</param>
    public sealed record Failed(AuthenticationFailureReason Reason) : AuthenticationOutcome;
}