namespace DMO.Application.Access;

/// <summary>
/// Discriminated result of resolving one persisted Module identity against the registry.
/// </summary>
/// <remarks>
/// The four layers are kept strictly separate: canonical vocabulary (known), registered
/// availability (available), persisted Template selection, and effective resolved access.
/// An <see cref="Unknown"/> or <see cref="KnownUnavailable"/> entry never becomes valid
/// runtime access: the Access Resolver denies the <b>entire</b> resolution for either.
/// </remarks>
public abstract record ModuleResolve
{
    private ModuleResolve()
    {
    }

    /// <summary>The persisted identity is canonical and registered/available in this build.</summary>
    /// <param name="Definition">The available Module definition.</param>
    public sealed record Available(ModuleDefinition Definition) : ModuleResolve;

    /// <summary>
    /// The persisted identity is canonical but not registered/available in this build
    /// (a future Module surface that is not yet implemented).
    /// </summary>
    /// <param name="Id">The known canonical identity.</param>
    public sealed record KnownUnavailable(ModuleId Id) : ModuleResolve;

    /// <summary>
    /// The persisted value is not a valid/known canonical Module identity (corrupt or
    /// incompatible persisted state).
    /// </summary>
    /// <param name="PersistedModuleId">The raw persisted value.</param>
    public sealed record Unknown(string PersistedModuleId) : ModuleResolve;
}