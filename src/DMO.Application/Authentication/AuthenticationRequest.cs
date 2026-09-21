namespace DMO.Application.Authentication;

/// <summary>
/// Base type of the typed login requests accepted by the authentication boundary.
/// </summary>
/// <remarks>
/// The two flows are genuinely separate: the USER contract is <c>company_number + password</c>
/// and the ADMIN contract is <c>email + password</c>. Email is never a USER login identifier
/// and <c>company_number</c> is never a Supabase email.
/// </remarks>
public abstract record AuthenticationRequest
{
    // Closed hierarchy: derivation is possible only inside DMO.Application, and the two
    // settled contracts below are the only flows the boundary accepts.
    private protected AuthenticationRequest()
    {
    }
}

/// <summary>
/// USER login request. The canonical, presented-to-the-USER login identifier is the
/// company number — there is no email field on this type.
/// </summary>
/// <param name="CompanyNumber">Canonical USER login identifier.</param>
/// <param name="Password">Presented password. Never persisted.</param>
public sealed record UserLoginRequest(string CompanyNumber, string Password) : AuthenticationRequest;

/// <summary>
/// ADMIN login request. The ADMIN identifier is the ADMIN email; no other identifier
/// exists (no <c>AdminLoginName</c>).
/// </summary>
/// <param name="Email">Dedicated ADMIN login identifier.</param>
/// <param name="Password">Presented password. Never persisted.</param>
public sealed record AdminLoginRequest(string Email, string Password) : AuthenticationRequest;