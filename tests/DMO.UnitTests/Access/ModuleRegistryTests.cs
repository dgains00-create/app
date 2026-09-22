using DMO.Application.Access;
using DMO.UnitTests.Access.Fakes;

namespace DMO.UnitTests.Access;

/// <summary>
/// P1-T04 registry tests — canonical vocabulary, strict known-vs-available behaviour and
/// construction-time rejection of invalid/duplicate definitions.
/// </summary>
public sealed class ModuleRegistryTests
{
    private static readonly string[] MasterOrderIds =
    [
        "job-on-view",
        "job-on-create",
        "controlo-create",
        "controlo-approve",
        "reparacao-interna",
        "boquilhas",
        "armazem",
        "reparacao-programada-view",
        "reparacao-programada-create",
        "tampoes",
        "historia",
        "ferramentas",
        "ferramentas-approve",
    ];

    [Fact]
    public void Catalog_ExposesExactlyTheThirteenCanonicalModules_InMasterOrder()
    {
        var catalogIds = ModuleCatalog.All.Select(entry => entry.Id.Value).ToArray();

        Assert.Equal(MasterOrderIds, catalogIds);
        Assert.Equal(13, ModuleCatalog.All.Count);

        // Unique identities: exactly 13 distinct ModuleIds.
        Assert.Equal(13, ModuleCatalog.All.Select(entry => entry.Id).Distinct().Count());

        var registry = TestModuleDefinitions.TestRegistry();
        Assert.Equal(MasterOrderIds, registry.KnownModuleIds.Select(id => id.Value).ToArray());
    }

    [Fact]
    public void DisplayName_IsNotTheIdentity()
    {
        // Shared visible destination, distinct access identities.
        Assert.Equal("job-on", ModuleCatalog.All.Single(entry => entry.Id == ModuleCatalog.JobOnView).DestinationId);
        Assert.Equal("job-on", ModuleCatalog.All.Single(entry => entry.Id == ModuleCatalog.JobOnCreate).DestinationId);
        Assert.NotEqual(ModuleCatalog.JobOnView, ModuleCatalog.JobOnCreate);
        Assert.Equal(ModuleId.From("job-on-view"), ModuleCatalog.JobOnView);

        // Display-name text is not a Module identity value.
        Assert.NotEqual("Job On View", ModuleCatalog.JobOnView.Value);
        Assert.Null(ModuleCatalog.FindByValue("Job On View"));
        Assert.Equal(ModuleCatalog.JobOnView, ModuleCatalog.FindByValue("job-on-view"));
    }

    [Fact]
    public void DuplicateRegistration_FailsDeterministically()
    {
        var duplicates = new[] { TestModuleDefinitions.ControloCreate(), TestModuleDefinitions.ControloCreate() };

        var exception = Assert.Throws<ArgumentException>(() => ModuleRegistry.Create(duplicates));

        Assert.Contains("more than once", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnknownModuleId_Lookup_Fails()
    {
        var registry = TestModuleDefinitions.TestRegistry();

        var malformed = Assert.IsType<ModuleResolve.Unknown>(registry.Resolve("nao-existe"));
        Assert.Equal("nao-existe", malformed.PersistedModuleId);

        // Not valid kebab-case / not canonical → Unknown, never a grant.
        Assert.IsType<ModuleResolve.Unknown>(registry.Resolve("Not Kebab!"));
        Assert.IsType<ModuleResolve.Unknown>(registry.Resolve("controlo approve"));
        Assert.IsType<ModuleResolve.Unknown>(registry.Resolve("controlo-approve "));
    }

    [Fact]
    public void UnavailableModule_IsNotSelectable()
    {
        // armazem is canonical but deliberately NOT registered in the test availability list.
        var registry = TestModuleDefinitions.TestRegistry();

        var unavailable = Assert.IsType<ModuleResolve.KnownUnavailable>(registry.Resolve("armazem"));
        Assert.Equal(ModuleCatalog.Armazem, unavailable.Id);

        Assert.False(registry.IsAvailable(ModuleCatalog.Armazem));
        Assert.DoesNotContain(registry.AvailableModules, definition => definition.Id == ModuleCatalog.Armazem);
    }

    [Fact]
    public void ContextualOnlyModule_MayHaveNoDestination()
    {
        var ferramentas = TestModuleDefinitions.Ferramentas();

        Assert.True(ferramentas.Surface.IsContextualOnly);
        Assert.Null(ferramentas.DestinationId);
        Assert.Null(ferramentas.Surface.DestinationId);
        Assert.Null(ModuleCatalog.All.Single(entry => entry.Id == ModuleCatalog.Ferramentas).DestinationId);

        // Registered contextual-only Modules resolve as Available with no destination.
        var registry = TestModuleDefinitions.TestRegistry();
        var available = Assert.IsType<ModuleResolve.Available>(registry.Resolve("ferramentas"));
        Assert.Equal(ModuleCatalog.Ferramentas, available.Definition.Id);
        Assert.Null(available.Definition.DestinationId);
    }

    [Fact]
    public void SharedDestinationModules_RemainSeparateDefinitions()
    {
        var registry = TestModuleDefinitions.TestRegistry();

        var view = Assert.IsType<ModuleResolve.Available>(registry.Resolve("job-on-view")).Definition;
        var create = Assert.IsType<ModuleResolve.Available>(registry.Resolve("job-on-create")).Definition;

        Assert.Equal("job-on", view.DestinationId);
        Assert.Equal("job-on", create.DestinationId);
        Assert.NotEqual(view.Id, create.Id);

        // The identity is the access unit: the view definition never satisfies create.
        Assert.NotEqual(ModuleCatalog.JobOnCreate, view.Id);
        Assert.NotEqual(ModuleCatalog.ControloCreate, TestModuleDefinitions.ControloApprove().Id);
    }

    [Fact]
    public void InvalidDefinitionId_IsRejected_AtConstruction()
    {
        Assert.ThrowsAny<ArgumentException>(() => ModuleRegistry.Create(
        [
            new ModuleDefinition(
                ModuleId.From("nao-existe"), // valid kebab-case but NOT canonical
                "Nao Existe",
                "nao-existe",
                IsDefaultLandingEligible: false,
                new ModuleSurfaceDescriptor("Nao", "nao-existe", IsContextualOnly: false, [])),
        ]));
    }

    [Fact]
    public void InconsistentDestinationMetadata_IsRejected_AtRegistryConstruction()
    {
        // Top-level Module (not contextual-only) must declare a destination.
        Assert.ThrowsAny<ArgumentException>(() => ModuleRegistry.Create(
        [
            new ModuleDefinition(
                ModuleCatalog.ControloCreate,
                "Controlo Create",
                null,
                IsDefaultLandingEligible: false,
                new ModuleSurfaceDescriptor("Controlo", null, IsContextualOnly: false, [])),
        ]));

        // Contextual-only Module must NOT declare a top-level destination.
        Assert.ThrowsAny<ArgumentException>(() => ModuleRegistry.Create(
        [
            new ModuleDefinition(
                ModuleCatalog.Ferramentas,
                "Ferramentas",
                "ferramentas",
                IsDefaultLandingEligible: false,
                new ModuleSurfaceDescriptor("Ferramentas", "ferramentas", IsContextualOnly: true, [])),
        ]));
    }

    [Fact]
    public void AvailableModules_AreOrderedDeterministically()
    {
        // Registration order deliberately differs from canonical-id order.
        var registry = ModuleRegistry.Create(
        [
            TestModuleDefinitions.JobOnCreate(),
            TestModuleDefinitions.ControloApprove(),
            TestModuleDefinitions.ControloCreate(),
            TestModuleDefinitions.JobOnView(),
        ]);

        Assert.Equal(
            new[] { "controlo-approve", "controlo-create", "job-on-create", "job-on-view" },
            registry.AvailableModules.Select(definition => definition.Id.Value).ToArray());
    }

    [Fact]
    public void Empty_ExposesFullVocabulary_AndNothingAvailable()
    {
        // Phase 1 production posture: full vocabulary, zero available Modules.
        var registry = ModuleRegistry.Empty();

        Assert.Equal(13, registry.KnownModuleIds.Count);
        Assert.Empty(registry.AvailableModules);
        Assert.All(registry.KnownModuleIds, id => Assert.False(registry.IsAvailable(id)));

        var unavailable = Assert.IsType<ModuleResolve.KnownUnavailable>(registry.Resolve("controlo-approve"));
        Assert.Equal(ModuleCatalog.ControloApprove, unavailable.Id);
    }
}