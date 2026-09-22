using DMO.Application.Access;

namespace DMO.UnitTests.Access.Fakes;

/// <summary>
/// Controlled test Module definitions (request §14: tests may instantiate controlled test
/// registries with selected available definitions).
/// </summary>
/// <remarks>
/// Only the Modules needed to prove the mechanics are made available here. <c>armazem</c> is
/// deliberately <b>not</b> registered, so it acts as the canonical known-but-unavailable
/// Module of the tests.
/// </remarks>
public static class TestModuleDefinitions
{
    /// <summary>Job On View — read/consultation surface, destination <c>job-on</c>.</summary>
    public static ModuleDefinition JobOnView() => new(
        ModuleCatalog.JobOnView,
        "Job On View",
        "job-on",
        IsDefaultLandingEligible: true,
        new ModuleSurfaceDescriptor(
            "Job On",
            "job-on",
            IsContextualOnly: false,
            ["read", "verificacao-confirm"]));

    /// <summary>Job On Create — create/edit/manage surface, destination <c>job-on</c>.</summary>
    public static ModuleDefinition JobOnCreate() => new(
        ModuleCatalog.JobOnCreate,
        "Job On Create",
        "job-on",
        IsDefaultLandingEligible: false,
        new ModuleSurfaceDescriptor(
            "Job On",
            "job-on",
            IsContextualOnly: false,
            ["create", "edit", "manage", "delete", "verificacoes-manage"]));

    /// <summary>Controlo Create — creation/measurement/submission surface, destination <c>controlo</c>.</summary>
    public static ModuleDefinition ControloCreate() => new(
        ModuleCatalog.ControloCreate,
        "Controlo Create",
        "controlo",
        IsDefaultLandingEligible: true,
        new ModuleSurfaceDescriptor(
            "Controlo",
            "controlo",
            IsContextualOnly: false,
            ["peso-create", "comparacao-create", "folha-prepare", "submission"]));

    /// <summary>Controlo Approve — review/approval/decision surface, destination <c>controlo</c>.</summary>
    public static ModuleDefinition ControloApprove() => new(
        ModuleCatalog.ControloApprove,
        "Controlo Approve",
        "controlo",
        IsDefaultLandingEligible: false,
        new ModuleSurfaceDescriptor(
            "Controlo",
            "controlo",
            IsContextualOnly: false,
            ["peso-review", "peso-approve", "peso-reject", "comparacao-decision", "folha-decision", "reopen", "send-to-production"]));

    /// <summary>Ferramentas — contextual-only access unit, no top-level destination.</summary>
    public static ModuleDefinition Ferramentas() => new(
        ModuleCatalog.Ferramentas,
        "Ferramentas",
        null,
        IsDefaultLandingEligible: false,
        new ModuleSurfaceDescriptor(
            "Ferramentas",
            null,
            IsContextualOnly: true,
            ["tool-view", "tool-create", "tool-change-request"]));

    /// <summary>Ferramentas Approve — contextual-only access unit, no top-level destination.</summary>
    public static ModuleDefinition FerramentasApprove() => new(
        ModuleCatalog.FerramentasApprove,
        "Ferramentas Approve",
        null,
        IsDefaultLandingEligible: false,
        new ModuleSurfaceDescriptor(
            "Ferramentas",
            null,
            IsContextualOnly: true,
            ["tool-approve", "tool-reject", "tool-edit-approve"]));

    /// <summary>
    /// The controlled test availability list: the Modules the resolver/gate tests treat as
    /// available in the current build. <c>armazem</c> (and every other canonical identity) is
    /// known but unavailable here.
    /// </summary>
    public static IReadOnlyList<ModuleDefinition> AllAvailable() =>
    [
        JobOnView(),
        JobOnCreate(),
        ControloCreate(),
        ControloApprove(),
        Ferramentas(),
        FerramentasApprove(),
    ];

    /// <summary>Creates a registry with the controlled test availability list.</summary>
    public static IModuleRegistry TestRegistry() => ModuleRegistry.Create(AllAvailable());
}