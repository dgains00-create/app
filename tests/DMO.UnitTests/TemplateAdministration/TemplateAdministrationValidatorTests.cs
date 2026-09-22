using DMO.Application.Access;
using DMO.Application.TemplateAdministration;
using DMO.UnitTests.Access.Fakes;

namespace DMO.UnitTests.TemplateAdministration;

/// <summary>
/// P1-T06 validator tests — the pure <see cref="TemplateAdministrationValidator"/> against
/// the controlled test registry (6 available Modules; <c>armazem</c> canonical but
/// unavailable): name bound, canonical-id-only composition, no duplicates, no new
/// unavailable selection, no unknown ids, persisted unavailable/invalid entries preserved or
/// explicitly removed (never silently repaired), and the landing rule (null OR represented by
/// a selected available non-contextual Module destination).
/// </summary>
public sealed class TemplateAdministrationValidatorTests
{
    private const string JobOnView = "job-on-view";
    private const string JobOnCreate = "job-on-create";
    private const string ControloCreate = "controlo-create";
    private const string ControloApprove = "controlo-approve";
    private const string Ferramentas = "ferramentas";
    private const string Armazem = "armazem"; // canonical, unavailable in the test registry

    // -------------------------------------------------------------- NAME

    [Fact]
    public void Create_BlankName_Error()
    {
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand("  ", [], LandingDestinationId: null),
            CreateRegistry());

        Assert.Contains(errors, error => error.Contains("Name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Create_TooLongName_Error()
    {
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand(
                new string('x', TemplateAdministrationValidator.MaxNameLength + 1),
                [],
                LandingDestinationId: null),
            CreateRegistry());

        Assert.Contains(errors, error => error.Contains("Name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Create_BlankComposition_NullLanding_NoErrors()
    {
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand(
                "Vazio", ModuleIds: [], LandingDestinationId: null),
            CreateRegistry());

        Assert.Empty(errors);
    }

    // -------------------------------------------------------------- COMPOSITION

    [Fact]
    public void Create_KnownAvailableModuleIds_NoErrors()
    {
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand(
                "Manutenção", [JobOnView, ControloCreate], LandingDestinationId: null),
            CreateRegistry());

        Assert.Empty(errors);
    }

    [Fact]
    public void Create_UnavailableModule_Error_NoNewSelection()
    {
        // armazem is canonical but not registered in this build: it can never be newly selected.
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand(
                "Manutenção", [Armazem], LandingDestinationId: null),
            CreateRegistry());

        Assert.Contains(errors, error => error.Contains(Armazem, StringComparison.Ordinal));
    }

    [Fact]
    public void Create_UnknownModuleId_Error()
    {
        // A display name or any non-canonical string is not an identity.
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand(
                "Manutenção", ["Job On View"], LandingDestinationId: null),
            CreateRegistry());

        Assert.Contains(errors, error => error.Contains("Job On View", StringComparison.Ordinal));
    }

    [Fact]
    public void Create_DuplicateModuleIds_Error()
    {
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand(
                "Manutenção", [JobOnView, JobOnView], LandingDestinationId: null),
            CreateRegistry());

        Assert.Contains(errors, error => error.Contains("more than once", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Create_DisplayNameIsNotIdentity_Errors()
    {
        // The validator never resolves display names/destinations as identities (ACCESS_MODEL §2).
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand(
                "Manutenção", ["Job On"], LandingDestinationId: null),
            CreateRegistry());

        Assert.DoesNotContain(errors, error => error.Contains("Job On View", StringComparison.Ordinal));
    }

    [Fact]
    public void Update_PersistedUnavailableModule_Preserved_NoErrors()
    {
        // A persisted unavailable entry (future surface, or state created while available)
        // may stay: the ficha surfaces it locked; this update preserves it (no silent repair).
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.UpdateTemplateCommand(
                Guid.NewGuid(), "Manutenção", [JobOnView, Armazem], LandingDestinationId: null, ExpectedVersion: 1),
            persistedModuleIds: new HashSet<string>([JobOnView, Armazem], StringComparer.Ordinal),
            registry: CreateRegistry());

        Assert.Empty(errors);
    }

    [Fact]
    public void Update_ReAddingUnavailableModule_Error()
    {
        // Re-adding a known-but-unavailable id that is NOT in the persisted composition is a
        // new selection → rejected.
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.UpdateTemplateCommand(
                Guid.NewGuid(), "Manutenção", [Armazem], LandingDestinationId: null, ExpectedVersion: 1),
            persistedModuleIds: new HashSet<string>([JobOnView], StringComparer.Ordinal),
            registry: CreateRegistry());

        Assert.Contains(errors, error => error.Contains(Armazem, StringComparison.Ordinal));
    }

    [Fact]
    public void Update_RemovingLastUnavailableModule_Allowed()
    {
        // Explicit removal is always allowed: the composition simply no longer contains it.
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.UpdateTemplateCommand(
                Guid.NewGuid(), "Manutenção", [JobOnView], LandingDestinationId: null, ExpectedVersion: 1),
            persistedModuleIds: new HashSet<string>([JobOnView, Armazem], StringComparer.Ordinal),
            registry: CreateRegistry());

        Assert.Empty(errors);
    }

    // -------------------------------------------------------------- LANDING

    [Fact]
    public void Create_NullLanding_Valid()
    {
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand(
                "Manutenção", [JobOnView], LandingDestinationId: null),
            CreateRegistry());

        Assert.Empty(errors);
    }

    [Fact]
    public void Create_LandingRepresentedBySelectedModule_Valid()
    {
        // job-on-view has destination "job-on": a represented landing.
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand(
                "Manutenção", [JobOnView], LandingDestinationId: "job-on"),
            CreateRegistry());

        Assert.Empty(errors);
    }

    [Fact]
    public void Create_SharedDestination_Valid_WithoutModuleMerging()
    {
        // job-on-view + job-on-create share destination "job-on" but stay two distinct Module
        // identities: the landing is valid and no identity merging occurs.
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand(
                "Manutenção", [JobOnView, JobOnCreate], LandingDestinationId: "job-on"),
            CreateRegistry());

        Assert.Empty(errors);
    }

    [Fact]
    public void Create_LandingNotRepresented_Error()
    {
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand(
                "Manutenção", [JobOnView], LandingDestinationId: "controlo"),
            CreateRegistry());

        Assert.Contains(errors, error => error.Contains("controlo", StringComparison.Ordinal));
    }

    [Fact]
    public void Create_LandingOnlyFromContextualModule_Error()
    {
        // ferramentas is contextual-only (no destination): it cannot represent any landing.
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand(
                "Manutenção", [Ferramentas], LandingDestinationId: "job-on"),
            CreateRegistry());

        Assert.Contains(errors, error => error.Contains("job-on", StringComparison.Ordinal));
    }

    [Fact]
    public void Create_LandingNotBackedByAvailableModule_Error()
    {
        // The landing expects "job-on", but the only selected Module is armazem (unavailable):
        // unavailable Modules never back a landing (they grant nothing).
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand(
                "Manutenção", [Armazem], LandingDestinationId: "job-on"),
            CreateRegistry());

        Assert.Contains(errors, error => error.Contains("job-on", StringComparison.Ordinal));
    }

    [Fact]
    public void Update_RemovingModuleThatBacksLanding_Error()
    {
        // The edit drops the only Module with destination "job-on" but keeps the landing:
        // rejected (no silent clearing, no auto-fix) — the Admin must change/remove the landing.
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.UpdateTemplateCommand(
                Guid.NewGuid(), "Manutenção", [ControloCreate], LandingDestinationId: "job-on", ExpectedVersion: 1),
            persistedModuleIds: new HashSet<string>([JobOnView, ControloCreate], StringComparer.Ordinal),
            registry: CreateRegistry());

        Assert.Contains(errors, error => error.Contains("job-on", StringComparison.Ordinal));
    }

    [Fact]
    public void Update_CorrectingLandingWhileRemovingModule_Valid()
    {
        // The Admin explicitly corrects the landing to the represented destination.
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.UpdateTemplateCommand(
                Guid.NewGuid(), "Manutenção", [ControloCreate], LandingDestinationId: "controlo", ExpectedVersion: 1),
            persistedModuleIds: new HashSet<string>([JobOnView, ControloCreate], StringComparer.Ordinal),
            registry: CreateRegistry());

        Assert.Empty(errors);
    }

    // -------------------------------------------------------------- HELPERS

    private static IModuleRegistry CreateRegistry() => TestModuleDefinitions.TestRegistry();
}