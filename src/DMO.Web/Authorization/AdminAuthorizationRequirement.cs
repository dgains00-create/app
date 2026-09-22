using Microsoft.AspNetCore.Authorization;

namespace DMO.Web.Authorization;

/// <summary>
/// Marker requirement for the single ADMIN-only administration policy
/// (<see cref="AdministrationAuthorizationPolicies.PolicyName"/>).
/// </summary>
/// <remarks>
/// The requirement carries no fact: "administration" is not a Module, not a role label, not a
/// Template and not a claim. The handler decides from the accepted current-account boundary
/// alone.
/// </remarks>
public sealed class AdminAuthorizationRequirement : IAuthorizationRequirement
{
}