namespace DMO.Application.Authentication;

/// <summary>
/// Credential-carrier identity resolved for a USER login request.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CarrierEmail"/> is the <b>provider credential carrier</b> only — a provisioned
/// account attribute (<c>users.email</c>, schema-enforced NOT NULL) used by the Supabase
/// password grant. It is an internal provider mechanic, never the USER-facing login
/// identifier and never shown to the USER as their login.
/// </para>
/// </remarks>
/// <param name="CarrierEmail">The provider credential carrier email of the USER account.</param>
public sealed record UserLoginIdentity(string CarrierEmail);