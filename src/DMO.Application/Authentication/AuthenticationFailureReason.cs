namespace DMO.Application.Authentication;

/// <summary>
/// Authentication-layer failure reasons.
/// </summary>
/// <remarks>
/// Only provider/credential facts belong here. Unknown application account, inactive ADMIN,
/// inactive USER, ambiguous mapping and missing Template are account-resolution facts and are
/// deliberately impossible to express at this layer.
/// </remarks>
public enum AuthenticationFailureReason
{
    /// <summary>The provider rejected the presented credentials.</summary>
    InvalidCredentials,

    /// <summary>
    /// The provider is unreachable, timed out, rate-limited, or not configured for the
    /// requested flow. In P1-T02 the USER provider flow does not exist by declaration
    /// (the durable mapping is P1-T03), so every USER login attempt is reported this way.
    /// </summary>
    ProviderUnavailable,

    /// <summary>The provider returned a server-side error or an unparseable response.</summary>
    ProviderError,
}