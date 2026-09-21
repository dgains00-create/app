namespace DMO.Application.Authentication;

/// <summary>
/// Minimum mechanical routing discriminator that tells account resolution which application
/// boundary the authenticated identity came through.
/// </summary>
/// <remarks>
/// This carries zero access meaning. It never classifies ADMIN/USER by itself: the account
/// resolver still requires an explicit application mapping for the authenticated identity.
/// </remarks>
public enum AuthenticationPath
{
    /// <summary>The identity was established through the dedicated ADMIN flow (email + password).</summary>
    Admin,

    /// <summary>The identity was established through the USER flow (company_number + password).</summary>
    User,
}