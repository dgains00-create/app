namespace DMO.Application.Authentication;

/// <summary>
/// Narrow persistence dependency consumed by the authentication boundary for the USER
/// <c>company_number + password</c> flow.
/// </summary>
/// <remarks>
/// <para>
/// The boundary never touches EF or storage directly: it resolves the USER's provider
/// credential carrier (the provisioned carrier email for <c>company_number</c>) through this
/// narrow contract only. The <c>company_number</c> is never converted into an email, never
/// sent to the provider as an email, and no synthetic email is ever built.
/// </para>
/// <para>
/// Implemented in <c>DMO.Infrastructure</c> (<c>UserAuthenticationLookup</c>) against
/// <c>users</c>; the Web authentication adapter depends on this DMO.Application contract, not
/// on EF.
/// </para>
/// </remarks>
public interface IUserAuthenticationLookup
{
    /// <summary>
    /// Resolves the carrier email for the given unique company number.
    /// </summary>
    /// <param name="companyNumber">The canonical USER-facing login identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The carrier login identity when the company number maps to a persisted USER row, or
    /// <c>null</c> when the company number is unknown.
    /// </returns>
    Task<UserLoginIdentity?> GetByCompanyNumberAsync(
        string companyNumber,
        CancellationToken cancellationToken);
}