using DMO.Application.Access;
using Microsoft.AspNetCore.Authorization;

namespace DMO.Web.Authorization;

/// <summary>
/// Authorization requirement representing <i>requires canonical Module X</i>.
/// </summary>
/// <remarks>
/// The requirement carries exactly one fact — a canonical <see cref="ModuleId"/> from the
/// <see cref="ModuleCatalog"/>. It is a thin projection of Module identity and never a second
/// permission catalogue: no capability labels, no role/profile names, no raw permissions.
/// </remarks>
public sealed class ModuleAuthorizationRequirement : IAuthorizationRequirement
{
    /// <summary>Creates a requirement for the given canonical Module.</summary>
    /// <param name="requiredModule">The canonical Module identity required by the gate.</param>
    public ModuleAuthorizationRequirement(ModuleId requiredModule)
    {
        RequiredModule = requiredModule;
    }

    /// <summary>The canonical Module identity required by the gate.</summary>
    public ModuleId RequiredModule { get; }
}