namespace DMO.Application.Access;

/// <summary>
/// The canonical, code-owned Module Registry of the current build.
/// </summary>
/// <remarks>
/// <para>
/// The registry separates the canonical identity vocabulary (<see cref="KnownModuleIds"/>,
/// exactly the 13 Modules of the Master contract) from the Modules actually registered and
/// available in this build (<see cref="AvailableModules"/>). Knowing an identity is never the
/// same as exposing it as selectable/enforceable: a Module becomes available only when its
/// real functional surface is implemented and its definition is explicitly registered
/// (ACCESS_MODEL §6).
/// </para>
/// <para>
/// Definitions are an explicit, auditable code list — no database definition table, no
/// reflection/plugin/assembly scanning, no Admin-created Module types.
/// </para>
/// </remarks>
public interface IModuleRegistry
{
    /// <summary>The canonical Module identity vocabulary (13, Master order) — audit surface.</summary>
    IReadOnlyList<ModuleId> KnownModuleIds { get; }

    /// <summary>The Module definitions registered/available in this build (selectable/enforceable).</summary>
    IReadOnlyList<ModuleDefinition> AvailableModules { get; }

    /// <summary>
    /// Resolves one persisted Module identity: <see cref="ModuleResolve.Available"/>,
    /// <see cref="ModuleResolve.KnownUnavailable"/>, or <see cref="ModuleResolve.Unknown"/>.
    /// </summary>
    ModuleResolve Resolve(string persistedModuleId);

    /// <summary>Returns whether the given canonical Module identity is registered/available.</summary>
    bool IsAvailable(ModuleId id);
}