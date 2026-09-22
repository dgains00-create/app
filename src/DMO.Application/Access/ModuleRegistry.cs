namespace DMO.Application.Access;

/// <summary>
/// Immutable runtime implementation of <see cref="IModuleRegistry"/>.
/// </summary>
/// <remarks>
/// <para>
/// Construction is the validation boundary: an invalid definition can never become valid
/// runtime access. The constructor rejects, with <see cref="ArgumentException"/>:
/// </para>
/// <list type="bullet">
/// <item>a definition whose <see cref="ModuleDefinition.Id"/> is not part of the canonical
/// <see cref="ModuleCatalog"/> (identities are code-owned; nobody invents Modules);</item>
/// <item>a definition whose destination metadata is inconsistent with its surface
/// (contextual-only without destination / top-level without destination);</item>
/// <item>a duplicate available Module registration (same canonical id more than once).</item>
/// </list>
/// <para>
/// The registry is deterministic and stable: <see cref="AvailableModules"/> is exposed in
/// canonical-id order. This order is presentation/audit ordering only and is never an
/// authorization factor; effective access order always follows the persisted Template
/// presentation order.
/// </para>
/// </remarks>
public sealed class ModuleRegistry : IModuleRegistry
{
    private readonly IReadOnlyDictionary<ModuleId, ModuleDefinition> _availableById;
    private readonly IReadOnlyList<ModuleDefinition> _availableInIdOrder;

    /// <summary>Creates a registry over the explicitly registered available Module definitions.</summary>
    /// <exception cref="ArgumentException">When the definitions contain an invalid identity, invalid destination metadata, or a duplicate id.</exception>
    public ModuleRegistry(IReadOnlyList<ModuleDefinition> availableModules)
    {
        ArgumentNullException.ThrowIfNull(availableModules);

        // Deterministic canonical-id ordering of the explicit registration list.
        var sorted = availableModules
            .OrderBy(definition => definition.Id.Value, StringComparer.Ordinal)
            .ToArray();

        var byId = new Dictionary<ModuleId, ModuleDefinition>(availableModules.Count);
        foreach (var definition in sorted)
        {
            if (!ModuleCatalog.IsKnown(definition.Id))
            {
                throw new ArgumentException(
                    $"Module id '{definition.Id.Value}' is not part of the canonical ModuleCatalog; identities are code-owned.",
                    nameof(availableModules));
            }

            if (definition.Surface.IsContextualOnly != (definition.DestinationId is null))
            {
                throw new ArgumentException(
                    $"Module '{definition.Id.Value}' has inconsistent destination metadata for its surface descriptor.",
                    nameof(availableModules));
            }

            if (!byId.TryAdd(definition.Id, definition))
            {
                throw new ArgumentException(
                    $"Module '{definition.Id.Value}' is registered more than once; duplicate available Module registration is invalid.",
                    nameof(availableModules));
            }
        }

        _availableById = byId;
        _availableInIdOrder = sorted;
    }

    /// <summary>Creates a registry over the given definitions.</summary>
    public static ModuleRegistry Create(IReadOnlyList<ModuleDefinition> availableModules) =>
        new(availableModules);

    /// <summary>
    /// Creates the Phase 1 production registry: full canonical vocabulary, zero available
    /// Modules (no industrial surface is implemented yet — nothing is granted).
    /// </summary>
    public static ModuleRegistry Empty() => new([]);

    /// <inheritdoc />
    public IReadOnlyList<ModuleId> KnownModuleIds { get; } =
        ModuleCatalog.All.Select(entry => entry.Id).ToArray();

    /// <inheritdoc />
    public IReadOnlyList<ModuleDefinition> AvailableModules => _availableInIdOrder;

    /// <inheritdoc />
    public ModuleResolve Resolve(string persistedModuleId)
    {
        if (!ModuleId.TryParse(persistedModuleId, out var id))
        {
            return new ModuleResolve.Unknown(persistedModuleId);
        }

        if (!ModuleCatalog.IsKnown(id))
        {
            return new ModuleResolve.Unknown(persistedModuleId);
        }

        return _availableById.TryGetValue(id, out var definition)
            ? new ModuleResolve.Available(definition)
            : new ModuleResolve.KnownUnavailable(id);
    }

    /// <inheritdoc />
    public bool IsAvailable(ModuleId id) => _availableById.ContainsKey(id);
}