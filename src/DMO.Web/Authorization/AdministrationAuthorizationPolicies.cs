using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace DMO.Web.Authorization;

/// <summary>
/// The single ADMIN-only authorization policy for the USER administration surface (P1-T05).
/// </summary>
/// <remarks>
/// <para>
/// Administration is ADMIN-account functionality, <b>not</b> a USER Module: the policy name
/// (<c>dmo.administration</c>) is deliberately outside the deterministic
/// <c>dmo.module.&lt;id&gt;</c> namespace produced by
/// <see cref="ModuleAuthorizationPolicies"/>, and it can never collide with a Module policy.
/// No operational Module availability is touched (P1-T04 registry remains unchanged).
/// </para>
/// <para>
/// The policy contains exactly one <see cref="AdminAuthorizationRequirement"/>; the handler
/// (<see cref="AdminAuthorizationHandler"/>, scoped) is the only decision maker.
/// </para>
/// </remarks>
public static class AdministrationAuthorizationPolicies
{
    /// <summary>
    /// Policy name for the ADMIN-only administration gate. Explicitly not a Module policy
    /// name (<c>dmo.module.*</c>).
    /// </summary>
    public const string PolicyName = "dmo.administration";

    /// <summary>
    /// Registers the administration policy and its scoped handler.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <remarks>
    /// <c>AddAuthorization</c> places this policy onto the same authorization options used by
    /// the Module policies registration (options configure actions accumulate); the
    /// <c>dmo.administration</c> policy exists exactly once.
    /// </remarks>
    public static IServiceCollection AddAdministrationAuthorization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthorization(options =>
            options.AddPolicy(PolicyName, policy =>
                policy.Requirements.Add(new AdminAuthorizationRequirement())));

        // Scoped: the handler consumes the scoped ICurrentAccountContext.
        services.AddScoped<IAuthorizationHandler, AdminAuthorizationHandler>();

        return services;
    }
}