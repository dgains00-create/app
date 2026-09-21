namespace DMO.Application.Authentication;

/// <summary>
/// Minimal authenticated identity produced by the authentication boundary.
/// </summary>
/// <remarks>
/// <para>
/// The provider identity is <b>internal linkage only</b> and is never access authority.
/// </para>
/// <para>
/// There is deliberately no display name, email, role, claim or provider metadata on this
/// type: those are application account facts that belong to the account records produced by
/// account resolution, never to the authenticated identity.
/// </para>
/// <para>
/// No session, access, refresh or provider token ever appears here.
/// </para>
/// </remarks>
/// <param name="ProviderSubject">Stable provider identity key (internal linkage only).</param>
/// <param name="AuthenticationPath">Minimum mechanical routing discriminator, not an access claim.</param>
public sealed record AuthenticatedIdentity(
    string ProviderSubject,
    AuthenticationPath AuthenticationPath);