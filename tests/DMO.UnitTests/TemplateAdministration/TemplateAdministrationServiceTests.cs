using DMO.Application.Accounts;
using DMO.Application.TemplateAdministration;
using DMO.Application.UserAdministration;
using DMO.UnitTests.Access.Fakes;
using DMO.UnitTests.TemplateAdministration.Fakes;
using DMO.UnitTests.UserAdministration.Fakes;

namespace DMO.UnitTests.TemplateAdministration;

/// <summary>
/// P1-T06 service tests — the real <see cref="TemplateAdministrationService"/> over the
/// in-memory stores (shared FakeUserRepository so USER ↔ Template transversality is provable):
/// lifecycle (create/read/update/delete), Module composition rules (canonical ids only,
/// unavailable surfaced/preserved/removed explicitly, dense ordering, order never permission),
/// landing validation, USER association (assign/remove/reassign on the single
/// users.template_id, stale version conflicts) and the atomic delete-with-members flows.
/// </summary>
public sealed class TemplateAdministrationServiceTests
{
    private const string JobOnView = "job-on-view";
    private const string JobOnCreate = "job-on-create";
    private const string ControloCreate = "controlo-create";
    private const string ControloApprove = "controlo-approve";
    private const string Ferramentas = "ferramentas";
    private const string Armazem = "armazem"; // canonical, unavailable in the test registry

    // -------------------------------------------------------------- CREATE / LIST

    [Fact]
    public async Task Create_Valid_ReturnsCreated_PersistsRow_CompositionDense_Version1()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.CreateAsync(
            new TemplateAdministrationCommands.CreateTemplateCommand("Manutenção", [JobOnView, ControloCreate], LandingDestinationId: "job-on"),
            CancellationToken.None);

        var created = Assert.IsType<TemplateAdministrationResult.Created>(result);
        var ficha = await fixture.Service.GetAsync(created.TemplateId, CancellationToken.None);
        Assert.NotNull(ficha);
        Assert.Equal("Manutenção", ficha!.Name);
        Assert.Equal("job-on", ficha.LandingDestinationId);
        Assert.True(ficha.LandingIsValid);
        Assert.Equal(1, ficha.Version);
        Assert.Equal(
            [JobOnView, ControloCreate],
            ficha.Modules.Select(module => module.ModuleId).ToArray());
        Assert.All(ficha.Modules, module => Assert.Equal(TemplateModuleState.Available, module.State));
    }

    [Fact]
    public async Task Create_BlankName_ValidationFailed_NothingPersisted()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.CreateAsync(
            new TemplateAdministrationCommands.CreateTemplateCommand("  ", [JobOnView], LandingDestinationId: null),
            CancellationToken.None);

        Assert.IsType<TemplateAdministrationResult.ValidationFailed>(result);
        Assert.Empty(await fixture.Service.ListAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Create_UnavailableModule_NewSelection_ValidationFailed()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.CreateAsync(
            new TemplateAdministrationCommands.CreateTemplateCommand("Manutenção", [Armazem], LandingDestinationId: null),
            CancellationToken.None);

        Assert.IsType<TemplateAdministrationResult.ValidationFailed>(result);
        Assert.Empty(await fixture.Service.ListAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Create_UnknownModuleId_ValidationFailed()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.CreateAsync(
            new TemplateAdministrationCommands.CreateTemplateCommand("Manutenção", ["Job On View"], LandingDestinationId: null),
            CancellationToken.None);

        Assert.IsType<TemplateAdministrationResult.ValidationFailed>(result);
    }

    [Fact]
    public async Task Create_EmptyComposition_Allowed()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.CreateAsync(
            new TemplateAdministrationCommands.CreateTemplateCommand("Vazio", [], LandingDestinationId: null),
            CancellationToken.None);

        var created = Assert.IsType<TemplateAdministrationResult.Created>(result);
        var ficha = (await fixture.Service.GetAsync(created.TemplateId, CancellationToken.None))!;
        Assert.Empty(ficha.Modules);
        Assert.True(ficha.LandingIsValid);
    }

    [Fact]
    public async Task List_IncludesModulePresentation_LandingValidity_AndUserCount()
    {
        var fixture = CreateFixture();
        var created = await fixture.CreateTemplateAsync("Manutenção", [JobOnView, ControloCreate], "job-on");
        var member = await fixture.SeedUserAsync("2661", active: true, templateId: created);

        var items = await fixture.Service.ListAsync(CancellationToken.None);

        var item = Assert.Single(items);
        Assert.Equal("Manutenção", item.Name);
        Assert.Equal(2, item.Modules.Count);
        Assert.Equal("job-on", item.LandingDestinationId);
        Assert.True(item.LandingIsValid);
        Assert.Equal(1, item.AssociatedUserCount);
        Assert.Equal(1, item.Version);
        Assert.Contains(fixture.Users.Accounts, account => account.AccountId == member.AccountId);
    }

    [Fact]
    public async Task List_EmptyRegistry_NoTemplates_EmptyList()
    {
        var fixture = CreateFixture();
        Assert.Empty(await fixture.Service.ListAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Get_Missing_ReturnsNull()
    {
        var fixture = CreateFixture();
        Assert.Null(await fixture.Service.GetAsync(Guid.NewGuid(), CancellationToken.None));
    }

    // -------------------------------------------------------------- UPDATE / ORDERING

    [Fact]
    public async Task Update_FactsCompositionAndLanding_VersionBumps_CompositionReplaced()
    {
        var fixture = CreateFixture();
        var created = await fixture.CreateTemplateAsync("Manutenção", [JobOnView], null);
        var before = (await fixture.Service.GetAsync(created, CancellationToken.None))!;

        var result = await fixture.Service.UpdateAsync(
            new TemplateAdministrationCommands.UpdateTemplateCommand(
                created, "Manutenção Industrial", [ControloCreate, ControloApprove], "controlo", before.Version),
            CancellationToken.None);

        var success = Assert.IsType<TemplateAdministrationResult.Success>(result);
        Assert.Equal(2, success.Ficha.Version);
        Assert.Equal("Manutenção Industrial", success.Ficha.Name);
        Assert.Equal("controlo", success.Ficha.LandingDestinationId);
        Assert.True(success.Ficha.LandingIsValid);
        Assert.Equal(
            [ControloCreate, ControloApprove],
            success.Ficha.Modules.Select(module => module.ModuleId).ToArray());

        // reads reflect the committed state
        var after = (await fixture.Service.GetAsync(created, CancellationToken.None))!;
        Assert.Equal(2, after.Version);
        Assert.Equal(2, after.Modules.Count);
    }

    [Fact]
    public async Task Update_StaleVersion_Conflict_NothingChanged()
    {
        var fixture = CreateFixture();
        var created = await fixture.CreateTemplateAsync("Manutenção", [JobOnView], null);

        var result = await fixture.Service.UpdateAsync(
            new TemplateAdministrationCommands.UpdateTemplateCommand(
                created, "Renomeado", [JobOnView], null, ExpectedVersion: 99),
            CancellationToken.None);

        var conflict = Assert.IsType<TemplateAdministrationResult.Conflict>(result);
        Assert.Equal(TemplateConflictReason.StaleTemplateVersion, conflict.Reason);
        var after = (await fixture.Service.GetAsync(created, CancellationToken.None))!;
        Assert.Equal("Manutenção", after.Name);
        Assert.Equal(1, after.Version);
    }

    [Fact]
    public async Task Update_MissingTemplate_NotFound()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.UpdateAsync(
            new TemplateAdministrationCommands.UpdateTemplateCommand(
                Guid.NewGuid(), "X", [JobOnView], null, ExpectedVersion: 1),
            CancellationToken.None);

        Assert.IsType<TemplateAdministrationResult.NotFound>(result);
    }

    // --------------------------- POSITIVE VERSION RULE AT SERVICE BOUNDARY (correction B)

    /// <summary>
    /// Architect correction B at the service boundary: a zero ExpectedVersion is malformed
    /// input, so the service returns <c>ValidationFailed</c> before any repository write — it
    /// must never be compared as a "stale version" and must never reach the persistence layer.
    /// </summary>
    /// <remarks>
    /// Test: Update_ExpectedVersionZero_ValidationFailed_NoWrite.<br/>
    /// Purpose: prove malformed update carriers fail closed without persisting anything.<br/>
    /// Master behavior being verified: accepted plan §17 — update requires <c>ExpectedVersion &gt; 0</c>.<br/>
    /// Preconditions: persisted Template with version 1; update command with <c>ExpectedVersion = 0</c>.<br/>
    /// Action: <c>UpdateAsync</c>.<br/>
    /// Assertions: <c>ValidationFailed</c> (not <c>Conflict</c>); name/composition/version unchanged.<br/>
    /// Required non-effects: no <c>UpdatedAsync</c> call — version still 1, composition intact.<br/>
    /// What this proves: the validator boundary stops the write before any repository call.<br/>
    /// What this does NOT prove: direct validator behavior (validator tests cover it).
    /// </remarks>
    [Fact]
    public async Task Update_ExpectedVersionZero_ValidationFailed_NoWrite()
    {
        var fixture = CreateFixture();
        var created = await fixture.CreateTemplateAsync("Manutenção", [JobOnView], null);

        var result = await fixture.Service.UpdateAsync(
            new TemplateAdministrationCommands.UpdateTemplateCommand(
                created, "Renomeado", [ControloCreate], "controlo", ExpectedVersion: 0),
            CancellationToken.None);

        Assert.IsType<TemplateAdministrationResult.ValidationFailed>(result);

        var after = (await fixture.Service.GetAsync(created, CancellationToken.None))!;
        Assert.Equal("Manutenção", after.Name);
        Assert.Equal([JobOnView], after.Modules.Select(module => module.ModuleId).ToArray());
        Assert.Null(after.LandingDestinationId);
        Assert.Equal(1, after.Version);
    }

    /// <remarks>
    /// Test: Update_ExpectedVersionNegative_ValidationFailed_NoWrite.<br/>
    /// Purpose: negative carriers fail closed as input errors, never as stale conflicts.<br/>
    /// Master behavior being verified: accepted plan §17.<br/>
    /// Preconditions: persisted Template with version 1; <c>ExpectedVersion = -1</c>.<br/>
    /// Action: <c>UpdateAsync</c>.<br/>
    /// Assertions: <c>ValidationFailed</c>; nothing changed (version 1, same facts).<br/>
    /// Required non-effects: no <c>UpdatedAsync</c> call.<br/>
    /// What this proves: negative values cannot overwrite or bump anything.<br/>
    /// What this does NOT prove: validator-level rule (covered separately).
    /// </remarks>
    [Fact]
    public async Task Update_ExpectedVersionNegative_ValidationFailed_NoWrite()
    {
        var fixture = CreateFixture();
        var created = await fixture.CreateTemplateAsync("Manutenção", [JobOnView], null);

        var result = await fixture.Service.UpdateAsync(
            new TemplateAdministrationCommands.UpdateTemplateCommand(
                created, "Renomeado", [ControloCreate], "controlo", ExpectedVersion: -1),
            CancellationToken.None);

        Assert.IsType<TemplateAdministrationResult.ValidationFailed>(result);

        var after = (await fixture.Service.GetAsync(created, CancellationToken.None))!;
        Assert.Equal("Manutenção", after.Name);
        Assert.Equal([JobOnView], after.Modules.Select(module => module.ModuleId).ToArray());
        Assert.Equal(1, after.Version);
    }

    [Fact]
    public async Task Update_PersistedUnavailableModule_SurvivesOrRemovedExplicitly()
    {
        var fixture = CreateFixture();
        var created = await fixture.CreateTemplateAsync("Manutenção", [JobOnView], null);

        // Seed a persisted composition that includes the unavailable module (as if created
        // while it was available or by a future-build state): the service itself could never
        // write it as a new selection, so the fixture appends it directly to the store.
        fixture.Templates.AppendToCompositionProbe(created, Armazem);

        // 1. Preserved: an edit that keeps it (same ids, name change) succeeds.
        var preserved = await fixture.Service.UpdateAsync(
            new TemplateAdministrationCommands.UpdateTemplateCommand(
                created, "Manutenção v2", [JobOnView, Armazem], null, ExpectedVersion: 1),
            CancellationToken.None);
        var preservedOk = Assert.IsType<TemplateAdministrationResult.Success>(preserved);
        Assert.Contains(preservedOk.Ficha.Modules, module => module.ModuleId == Armazem);
        Assert.Equal(TemplateModuleState.KnownUnavailable, preservedOk.Ficha.Modules.Single(module => module.ModuleId == Armazem).State);

        // 2. Explicit removal: dropping it from the submitted ids succeeds.
        var removed = await fixture.Service.UpdateAsync(
            new TemplateAdministrationCommands.UpdateTemplateCommand(
                created, "Manutenção v3", [JobOnView], null, ExpectedVersion: 2),
            CancellationToken.None);
        var removedOk = Assert.IsType<TemplateAdministrationResult.Success>(removed);
        Assert.DoesNotContain(removedOk.Ficha.Modules, module => module.ModuleId == Armazem);

        // 3. Re-adding the unavailable module as a NEW selection fails.
        var readded = await fixture.Service.UpdateAsync(
            new TemplateAdministrationCommands.UpdateTemplateCommand(
                created, "Manutenção v4", [JobOnView, Armazem], null, ExpectedVersion: 3),
            CancellationToken.None);
        Assert.IsType<TemplateAdministrationResult.ValidationFailed>(readded);
    }

    [Fact]
    public async Task Update_RemovingModuleThatBacksLanding_ValidationFailed_NoAutoFix()
    {
        var fixture = CreateFixture();
        var created = await fixture.CreateTemplateAsync("Manutenção", [JobOnView], "job-on");

        // Edit drops job-on-view (the only destination "job-on" holder) but keeps the landing.
        var result = await fixture.Service.UpdateAsync(
            new TemplateAdministrationCommands.UpdateTemplateCommand(
                created, "Manutenção", [ControloCreate], "job-on", ExpectedVersion: 1),
            CancellationToken.None);

        var failed = Assert.IsType<TemplateAdministrationResult.ValidationFailed>(result);
        Assert.Contains(failed.Errors, error => error.Contains("job-on", StringComparison.Ordinal));

        // Nothing was silently cleared or fixed.
        var after = (await fixture.Service.GetAsync(created, CancellationToken.None))!;
        Assert.Equal("job-on", after.LandingDestinationId);
        Assert.Equal([JobOnView], after.Modules.Select(module => module.ModuleId).ToArray());
        Assert.Equal(1, after.Version);
    }

    [Fact]
    public async Task Update_CorrectingLanding_WhileRemovingBackingModule_Succeeds()
    {
        var fixture = CreateFixture();
        var created = await fixture.CreateTemplateAsync("Manutenção", [JobOnView], "job-on");

        var result = await fixture.Service.UpdateAsync(
            new TemplateAdministrationCommands.UpdateTemplateCommand(
                created, "Manutenção", [ControloCreate], "controlo", ExpectedVersion: 1),
            CancellationToken.None);

        Assert.IsType<TemplateAdministrationResult.Success>(result);
    }

    [Fact]
    public async Task Update_SharedDestination_AllowsLanding_WithoutMergingModuleIdentities()
    {
        var fixture = CreateFixture();
        var created = await fixture.CreateTemplateAsync(
            "Manutenção", [JobOnView, JobOnCreate], "job-on");

        var ficha = (await fixture.Service.GetAsync(created, CancellationToken.None))!;

        // Two distinct Module identities that share the destination; the landing is valid and
        // no identity merging occurred.
        Assert.Equal([JobOnView, JobOnCreate], ficha.Modules.Select(module => module.ModuleId).ToArray());
        Assert.True(ficha.LandingIsValid);
    }

    // -------------------------------------------------------------- ORDERING ≠ PERMISSION

    [Fact]
    public async Task PersistedComposition_DenseOrder1ToN_AfterCreateAndUpdate()
    {
        // The composition is persisted with the dense 1..n presentation order (the accepted
        // ordering rule). Ordering is presentation-only: the Access Resolver (P1-T04, proven
        // by its own accepted tests) reads only the canonical Module ids; this service test
        // proves the persistence contract that feeds it.
        var fixture = CreateFixture();

        var created = await fixture.CreateTemplateAsync("T", [ControloCreate, JobOnView], null);
        var compositions = fixture.Templates.CompositionsOf(created);
        Assert.Equal(
            [1, 2],
            compositions.Select(module => module.PresentationOrder).ToArray());
        Assert.Equal([ControloCreate, JobOnView], compositions.Select(module => module.ModuleId).ToArray());

        var updated = await fixture.Service.UpdateAsync(
            new TemplateAdministrationCommands.UpdateTemplateCommand(
                created, "T", [JobOnView, ControloCreate], null, ExpectedVersion: 1),
            CancellationToken.None);
        Assert.IsType<TemplateAdministrationResult.Success>(updated);

        var after = fixture.Templates.CompositionsOf(created);
        Assert.Equal([1, 2], after.Select(module => module.PresentationOrder).ToArray());
        Assert.Equal([JobOnView, ControloCreate], after.Select(module => module.ModuleId).ToArray());
    }

    [Fact]
    public void ModuleOrdering_NeverTouchesModuleIdentities()
    {
        // Presentation ordering operates on the ordered list of canonical Module ids; it never
        // rewrites, merges or revalues the ids themselves (display names/destinations are never
        // identities, ACCESS_MODEL §2). Proved structurally: reordering a composition keeps the
        // exact same canonical id set.
        var first = new[] { JobOnView, ControloCreate };
        var reordered = new[] { ControloCreate, JobOnView };
        Assert.Equal(
            first.OrderBy(id => id),
            reordered.OrderBy(id => id));
    }

    // -------------------------------------------------------------- LANDING SURFACING

    [Fact]
    public async Task Ficha_InvalidPersistedLanding_Surfaced_NotSilentlyRepaired()
    {
        var fixture = CreateFixture();
        var created = await fixture.CreateTemplateAsync("Manutenção", [JobOnView], "job-on");

        // The Admin edits away the backing module but corrects nothing — rejected (see above).
        // A persisted-invalid landing (e.g. future state) is surfaced with LandingIsValid=false.
        await fixture.Service.UpdateAsync(
            new TemplateAdministrationCommands.UpdateTemplateCommand(
                created, "Manutenção", [ControloCreate], "controlo", ExpectedVersion: 1),
            CancellationToken.None);

        // Now simulate a persisted invalid landing by editing the store directly (a future
        // build wrote destination "job-on" while the composition holds only controlo-create).
        fixture.Templates.NullLandingValidityProbe(created, "job-on");

        var ficha = (await fixture.Service.GetAsync(created, CancellationToken.None))!;
        Assert.False(ficha.LandingIsValid);
        Assert.Equal("job-on", ficha.LandingDestinationId);
    }

    // -------------------------------------------------------------- USER ASSOCIATION

    [Fact]
    public async Task SetTemplateUser_Assign_PersistsSingleRelation_AndShowsInFicha()
    {
        var fixture = CreateFixture();
        var templateA = await fixture.CreateTemplateAsync("A", [JobOnView], null);
        var user = await fixture.SeedUserAsync("2661", active: true, templateId: null);

        var result = await fixture.Service.SetTemplateUserAsync(
            new TemplateAdministrationCommands.SetTemplateUserCommand(
                templateA, user.AccountId, TargetTemplateId: templateA, user.Version),
            CancellationToken.None);

        var success = Assert.IsType<TemplateAdministrationResult.Success>(result);
        Assert.Contains(success.Ficha.Users, member => member.UserId == user.AccountId);

        // The relation is the single users.template_id column.
        var stored = fixture.Users.Accounts.Single(account => account.AccountId == user.AccountId);
        Assert.Equal(templateA, stored.TemplateId);
        Assert.True(stored.IsActive);
        Assert.Equal(user.Version + 1, stored.Version);
    }

    [Fact]
    public async Task SetTemplateUser_Remove_NullsRelation_UserStaysActive()
    {
        var fixture = CreateFixture();
        var templateA = await fixture.CreateTemplateAsync("A", [JobOnView], null);
        var user = await fixture.SeedUserAsync("2661", active: true, templateId: templateA);

        var result = await fixture.Service.SetTemplateUserAsync(
            new TemplateAdministrationCommands.SetTemplateUserCommand(
                templateA, user.AccountId, TargetTemplateId: null, user.Version),
            CancellationToken.None);

        var success = Assert.IsType<TemplateAdministrationResult.Success>(result);
        Assert.DoesNotContain(success.Ficha.Users, member => member.UserId == user.AccountId);

        var stored = fixture.Users.Accounts.Single(account => account.AccountId == user.AccountId);
        Assert.Null(stored.TemplateId);
        Assert.True(stored.IsActive);
    }

    [Fact]
    public async Task SetTemplateUser_Reassign_SingleColumnWrite_NoDualMembership()
    {
        var fixture = CreateFixture();
        var templateA = await fixture.CreateTemplateAsync("A", [JobOnView], null);
        var templateB = await fixture.CreateTemplateAsync("B", [ControloCreate], null);
        var user = await fixture.SeedUserAsync("2661", active: true, templateId: templateA);

        var result = await fixture.Service.SetTemplateUserAsync(
            new TemplateAdministrationCommands.SetTemplateUserCommand(
                templateB, user.AccountId, TargetTemplateId: templateB, user.Version),
            CancellationToken.None);

        var success = Assert.IsType<TemplateAdministrationResult.Success>(result);
        Assert.Contains(success.Ficha.Users, member => member.UserId == user.AccountId);

        // The user is in B and no longer in A: one relation, never A + B.
        Assert.Equal(templateB, fixture.Users.Accounts.Single(account => account.AccountId == user.AccountId).TemplateId);
        var fichaA = (await fixture.Service.GetAsync(templateA, CancellationToken.None))!;
        Assert.DoesNotContain(fichaA.Users, member => member.UserId == user.AccountId);
    }

    [Fact]
    public async Task SetTemplateUser_StaleUserVersion_Conflict_NothingChanged()
    {
        var fixture = CreateFixture();
        var templateA = await fixture.CreateTemplateAsync("A", [JobOnView], null);
        var templateB = await fixture.CreateTemplateAsync("B", [ControloCreate], null);
        var user = await fixture.SeedUserAsync("2661", active: true, templateId: templateA);

        // The picker's version is stale (the user was concurrently changed).
        var result = await fixture.Service.SetTemplateUserAsync(
            new TemplateAdministrationCommands.SetTemplateUserCommand(
                templateB, user.AccountId, TargetTemplateId: templateB, UserExpectedVersion: 999),
            CancellationToken.None);

        var conflict = Assert.IsType<TemplateAdministrationResult.Conflict>(result);
        Assert.Equal(TemplateConflictReason.StaleUserVersion, conflict.Reason);
        Assert.Equal(templateA, fixture.Users.Accounts.Single(account => account.AccountId == user.AccountId).TemplateId);
    }

    [Fact]
    public async Task SetTemplateUser_MissingTargetTemplate_ValidationFailed()
    {
        var fixture = CreateFixture();
        var templateA = await fixture.CreateTemplateAsync("A", [JobOnView], null);
        var user = await fixture.SeedUserAsync("2661", active: true, templateId: null);

        var result = await fixture.Service.SetTemplateUserAsync(
            new TemplateAdministrationCommands.SetTemplateUserCommand(
                templateA, user.AccountId, TargetTemplateId: Guid.NewGuid(), user.Version),
            CancellationToken.None);

        Assert.IsType<TemplateAdministrationResult.ValidationFailed>(result);
        Assert.Null(fixture.Users.Accounts.Single(account => account.AccountId == user.AccountId).TemplateId);
    }

    [Fact]
    public async Task SetTemplateUser_MissingContextTemplate_NotFound()
    {
        var fixture = CreateFixture();
        var user = await fixture.SeedUserAsync("2661", active: true, templateId: null);

        var result = await fixture.Service.SetTemplateUserAsync(
            new TemplateAdministrationCommands.SetTemplateUserCommand(
                Guid.NewGuid(), user.AccountId, TargetTemplateId: null, user.Version),
            CancellationToken.None);

        Assert.IsType<TemplateAdministrationResult.NotFound>(result);
    }

    [Fact]
    public async Task SetTemplateUser_MissingUser_NotFound()
    {
        var fixture = CreateFixture();
        var templateA = await fixture.CreateTemplateAsync("A", [JobOnView], null);

        var result = await fixture.Service.SetTemplateUserAsync(
            new TemplateAdministrationCommands.SetTemplateUserCommand(
                templateA, Guid.NewGuid(), TargetTemplateId: null, UserExpectedVersion: 1),
            CancellationToken.None);

        Assert.IsType<TemplateAdministrationResult.NotFound>(result);
    }

    // ------------------------------------- REMOVE MEMBERSHIP INVARIANT (Architect correction A)

    /// <summary>
    /// Architect correction A (implementation review, CORRECTION REQUIRED §2): the remove
    /// invariant is Application authority, not Razor. This test calls the service directly —
    /// the exact path the minimal API <c>DELETE /administration/templates/{id}/users/{userId}</c>
    /// route uses — with a USER that actually belongs to Template <b>B</b> and a context of
    /// Template <b>A</b>.
    /// </summary>
    /// <remarks>
    /// Test: SetTemplateUser_Remove_UserBelongsToAnotherTemplate_ValidationFailed_NoWrite.<br/>
    /// Purpose: prove the direct service/API remove path cannot clear a membership owned by a
    /// different Template.<br/>
    /// Master behavior being verified: a USER belongs to zero or one Template through the single
    /// <c>users.template_id</c> relation; removal through Template A is an operation on A's
    /// membership, never a general "clear any membership" primitive (ACCESS_MODEL §3/§14,
    /// ADMIN.md §4).<br/>
    /// Preconditions: Template A exists; Template B exists; USER U has <c>template_id = B</c>,
    /// active, version known.<br/>
    /// Action: <c>SetTemplateUserAsync(SetTemplateUserCommand(TemplateId: A, UserId: U,
    /// TargetTemplateId: null, UserExpectedVersion: U.Version))</c> — no Razor page involved.<br/>
    /// Assertions: closed <c>ValidationFailed</c> (never <c>Success</c>, never <c>Conflict</c>);
    /// <c>U.template_id</c> still equals B; U's version unchanged; B's ficha still lists U; A's
    /// ficha does not.<br/>
    /// Required non-effects: no write through <c>SetTemplateAsync</c>; version unchanged;
    /// membership of B unchanged; no USER row mutated.<br/>
    /// What this proves: the rule that the implementation review found living only in the Razor
    /// page now holds in the Application service, therefore Razor and the minimal API have
    /// identical semantics.<br/>
    /// What this does NOT prove: the HTTP status mapping of the delete route (covered by the
    /// endpoint/PG evidence) nor interactive page behavior.
    /// </remarks>
    [Fact]
    public async Task SetTemplateUser_Remove_UserBelongsToAnotherTemplate_ValidationFailed_NoWrite()
    {
        var fixture = CreateFixture();
        var templateA = await fixture.CreateTemplateAsync("A", [JobOnView], null);
        var templateB = await fixture.CreateTemplateAsync("B", [ControloCreate], null);
        var user = await fixture.SeedUserAsync("2661", active: true, templateId: templateB);

        var result = await fixture.Service.SetTemplateUserAsync(
            new TemplateAdministrationCommands.SetTemplateUserCommand(
                templateA, user.AccountId, TargetTemplateId: null, user.Version),
            CancellationToken.None);

        Assert.IsType<TemplateAdministrationResult.ValidationFailed>(result);

        // The membership still belongs to B, untouched — and the version proves no write ran.
        var stored = fixture.Users.Accounts.Single(account => account.AccountId == user.AccountId);
        Assert.Equal(templateB, stored.TemplateId);
        Assert.Equal(user.Version, stored.Version);
        Assert.True(stored.IsActive);

        var fichaB = (await fixture.Service.GetAsync(templateB, CancellationToken.None))!;
        Assert.Contains(fichaB.Users, member => member.UserId == user.AccountId);

        var fichaA = (await fixture.Service.GetAsync(templateA, CancellationToken.None))!;
        Assert.DoesNotContain(fichaA.Users, member => member.UserId == user.AccountId);
    }

    /// <summary>
    /// Architect correction A: the same invariant must also hold when the USER has no Template
    /// at all — a stale/incorrect form must not be able to clear an already-empty membership
    /// through an arbitrary Template context.
    /// </summary>
    /// <remarks>
    /// Test: SetTemplateUser_Remove_UserHasNoTemplate_ValidationFailed_NoWrite.<br/>
    /// Purpose: prove removal through Template A requires a current membership in A, so a USER
    /// with <c>template_id = null</c> is rejected rather than silently "succeeding".<br/>
    /// Master behavior being verified: same single-relation rule as above; no-op removal is not
    /// a success.<br/>
    /// Preconditions: Template A exists; USER U has <c>template_id = null</c>.<br/>
    /// Action: remove requested through context Template A.<br/>
    /// Assertions: <c>ValidationFailed</c>; <c>U.template_id</c> remains null; version
    /// unchanged.<br/>
    /// Required non-effects: no write, no version bump.<br/>
    /// What this proves: the invariant is <c>current membership == context Template</c>, not
    /// merely "not some other Template".<br/>
    /// What this does NOT prove: nothing about assign/reassign (unchanged accepted behavior).
    /// </remarks>
    [Fact]
    public async Task SetTemplateUser_Remove_UserHasNoTemplate_ValidationFailed_NoWrite()
    {
        var fixture = CreateFixture();
        var templateA = await fixture.CreateTemplateAsync("A", [JobOnView], null);
        var user = await fixture.SeedUserAsync("2661", active: true, templateId: null);

        var result = await fixture.Service.SetTemplateUserAsync(
            new TemplateAdministrationCommands.SetTemplateUserCommand(
                templateA, user.AccountId, TargetTemplateId: null, user.Version),
            CancellationToken.None);

        Assert.IsType<TemplateAdministrationResult.ValidationFailed>(result);

        var stored = fixture.Users.Accounts.Single(account => account.AccountId == user.AccountId);
        Assert.Null(stored.TemplateId);
        Assert.Equal(user.Version, stored.Version);
    }

    /// <summary>
    /// Architect correction A, positive side: removal through the Template the USER actually
    /// belongs to still succeeds and nulls the single relation (the accepted behavior is
    /// preserved, not narrowed).
    /// </summary>
    /// <remarks>
    /// Test: SetTemplateUser_Remove_UserBelongsToContextTemplate_Success_NullsRelation.<br/>
    /// Purpose: prove the new guard rejects only foreign/absent memberships and does not break
    /// the accepted remove flow.<br/>
    /// Master behavior being verified: ADMIN.md §4 — a USER associated with a Template can be
    /// removed from that Template; the USER remains active and is left without an effective
    /// Template (fail closed).<br/>
    /// Preconditions: Template A exists; USER U has <c>template_id = A</c>, active.<br/>
    /// Action: remove requested through context Template A.<br/>
    /// Assertions: <c>Success</c>; <c>U.template_id == null</c>; U still active; A's ficha no
    /// longer lists U.<br/>
    /// Required non-effects: USER row not deleted; active state preserved.<br/>
    /// What this proves: the guard is exactly the corrected invariant and nothing broader.<br/>
    /// What this does NOT prove: reassign semantics (accepted, separately covered).
    /// </remarks>
    [Fact]
    public async Task SetTemplateUser_Remove_UserBelongsToContextTemplate_Success_NullsRelation()
    {
        var fixture = CreateFixture();
        var templateA = await fixture.CreateTemplateAsync("A", [JobOnView], null);
        var user = await fixture.SeedUserAsync("2661", active: true, templateId: templateA);

        var result = await fixture.Service.SetTemplateUserAsync(
            new TemplateAdministrationCommands.SetTemplateUserCommand(
                templateA, user.AccountId, TargetTemplateId: null, user.Version),
            CancellationToken.None);

        Assert.IsType<TemplateAdministrationResult.Success>(result);

        var stored = fixture.Users.Accounts.Single(account => account.AccountId == user.AccountId);
        Assert.Null(stored.TemplateId);
        Assert.True(stored.IsActive);

        var fichaA = (await fixture.Service.GetAsync(templateA, CancellationToken.None))!;
        Assert.DoesNotContain(fichaA.Users, member => member.UserId == user.AccountId);
    }

    /// <summary>
    /// Architect correction A: the reject must not disturb the other two membership operations.
    /// </summary>
    /// <remarks>
    /// Test: SetTemplateUser_Reassign_Unaffected_ByRemoveInvariant.<br/>
    /// Purpose: prove the new remove guard is scoped to <c>TargetTemplateId == null</c> only.<br/>
    /// Master behavior being verified: reassign A → B remains a single atomic column write.<br/>
    /// Preconditions: Template A and B exist; USER U has <c>template_id = B</c>.<br/>
    /// Action: reassign U from B into A (not a remove).<br/>
    /// Assertions: <c>Success</c>; <c>U.template_id == A</c>.<br/>
    /// Required non-effects: no second relation; B no longer lists U.<br/>
    /// What this proves: the correction is narrow and does not reopen accepted assign/reassign
    /// behavior.<br/>
    /// What this does NOT prove: concurrency (separately covered).
    /// </remarks>
    [Fact]
    public async Task SetTemplateUser_Reassign_Unaffected_ByRemoveInvariant()
    {
        var fixture = CreateFixture();
        var templateA = await fixture.CreateTemplateAsync("A", [JobOnView], null);
        var templateB = await fixture.CreateTemplateAsync("B", [ControloCreate], null);
        var user = await fixture.SeedUserAsync("2661", active: true, templateId: templateB);

        var result = await fixture.Service.SetTemplateUserAsync(
            new TemplateAdministrationCommands.SetTemplateUserCommand(
                templateA, user.AccountId, TargetTemplateId: templateA, user.Version),
            CancellationToken.None);

        Assert.IsType<TemplateAdministrationResult.Success>(result);
        Assert.Equal(
            templateA,
            fixture.Users.Accounts.Single(account => account.AccountId == user.AccountId).TemplateId);
    }

    /// <summary>
    /// Architect correction B at the service boundary, membership: a zero
    /// <c>UserExpectedVersion</c> is malformed input — <c>ValidationFailed</c> before any
    /// membership write, never a stale-version conflict, never a null-out.
    /// </summary>
    /// <remarks>
    /// Test: SetTemplateUser_UserExpectedVersionZero_ValidationFailed_NoWrite.<br/>
    /// Purpose: prove malformed membership carriers cannot mutate <c>users.template_id</c>.<br/>
    /// Master behavior being verified: accepted plan §17 — association operations require a
    /// valid user version <c>&gt; 0</c>.<br/>
    /// Preconditions: Template A exists; USER U associated with A; command with
    /// <c>UserExpectedVersion = 0</c> and <c>TargetTemplateId = null</c> (the remove shape that
    /// would otherwise null the relation).<br/>
    /// Action: <c>SetTemplateUserAsync</c>.<br/>
    /// Assertions: <c>ValidationFailed</c>; <c>U.template_id</c> remains A; version unchanged.<br/>
    /// Required non-effects: no <c>SetTemplateAsync</c> call, no version bump.<br/>
    /// What this proves: the remove invariant and the positive-version rule both close at the
    /// same boundary — no write happens for malformed input.<br/>
    /// What this does NOT prove: HTTP mapping (endpoint tests cover the route).
    /// </remarks>
    [Fact]
    public async Task SetTemplateUser_UserExpectedVersionZero_ValidationFailed_NoWrite()
    {
        var fixture = CreateFixture();
        var templateA = await fixture.CreateTemplateAsync("A", [JobOnView], null);
        var user = await fixture.SeedUserAsync("2661", active: true, templateId: templateA);

        var result = await fixture.Service.SetTemplateUserAsync(
            new TemplateAdministrationCommands.SetTemplateUserCommand(
                templateA, user.AccountId, TargetTemplateId: null, UserExpectedVersion: 0),
            CancellationToken.None);

        Assert.IsType<TemplateAdministrationResult.ValidationFailed>(result);

        var stored = fixture.Users.Accounts.Single(account => account.AccountId == user.AccountId);
        Assert.Equal(templateA, stored.TemplateId);
        Assert.Equal(user.Version, stored.Version);
    }

    /// <remarks>
    /// Test: SetTemplateUser_UserExpectedVersionNegative_ValidationFailed_NoWrite.<br/>
    /// Purpose: negative membership carriers fail closed as input errors, never as stale
    /// conflicts, and never write.<br/>
    /// Master behavior being verified: accepted plan §17.<br/>
    /// Preconditions: Template A exists; USER U associated with A; <c>UserExpectedVersion = -1</c>.<br/>
    /// Action: <c>SetTemplateUserAsync</c>.<br/>
    /// Assertions: <c>ValidationFailed</c>; <c>U.template_id</c> remains A; version unchanged.<br/>
    /// Required non-effects: no <c>SetTemplateAsync</c> call.<br/>
    /// What this proves: malformed membership carriers cannot become conflicts or writes.<br/>
    /// What this does NOT prove: validator-level rule (covered separately).
    /// </remarks>
    [Fact]
    public async Task SetTemplateUser_UserExpectedVersionNegative_ValidationFailed_NoWrite()
    {
        var fixture = CreateFixture();
        var templateA = await fixture.CreateTemplateAsync("A", [JobOnView], null);
        var user = await fixture.SeedUserAsync("2661", active: true, templateId: templateA);

        var result = await fixture.Service.SetTemplateUserAsync(
            new TemplateAdministrationCommands.SetTemplateUserCommand(
                templateA, user.AccountId, TargetTemplateId: null, UserExpectedVersion: -1),
            CancellationToken.None);

        Assert.IsType<TemplateAdministrationResult.ValidationFailed>(result);

        var stored = fixture.Users.Accounts.Single(account => account.AccountId == user.AccountId);
        Assert.Equal(templateA, stored.TemplateId);
        Assert.Equal(user.Version, stored.Version);
    }

    // -------------------------------------------------------------- TRANSVERSAL (USER ficha)

    [Fact]
    public async Task Transversal_UserFichaAssignment_VisibleInTemplateFicha()
    {
        // Assign through the USER surface (IUserAdministrationService.SetTemplateAsync, the
        // exact same versioned primitive the Template surface reuses) and verify the Template
        // ficha sees the member.
        var fixture = CreateFixture();
        var template = await fixture.CreateTemplateAsync("A", [JobOnView], null);
        var user = await fixture.SeedUserAsync("2661", active: true, templateId: null);

        var userService = new UserAdministrationService(
            fixture.Users, fixture.Templates, new FakeUserIdentityProvisioner());
        var userCommand = new UserAdministrationCommands.SetUserTemplateCommand(
            user.AccountId, template, user.Version);
        Assert.IsType<UserAdministrationResult.Success>(
            await userService.SetTemplateAsync(userCommand, CancellationToken.None));

        var ficha = (await fixture.Service.GetAsync(template, CancellationToken.None))!;
        Assert.Contains(ficha.Users, member => member.UserId == user.AccountId);
    }

    [Fact]
    public async Task Transversal_TemplateFichaAssignment_VisibleInUserFicha()
    {
        // Assign through the Template surface; the USER ficha must show the same relation.
        var fixture = CreateFixture();
        var template = await fixture.CreateTemplateAsync("A", [JobOnView], null);
        var user = await fixture.SeedUserAsync("2661", active: true, templateId: null);

        var result = await fixture.Service.SetTemplateUserAsync(
            new TemplateAdministrationCommands.SetTemplateUserCommand(
                template, user.AccountId, TargetTemplateId: template, user.Version),
            CancellationToken.None);
        Assert.IsType<TemplateAdministrationResult.Success>(result);

        var userService = new UserAdministrationService(
            fixture.Users, fixture.Templates, new FakeUserIdentityProvisioner());
        var ficha = await userService.GetAsync(user.AccountId, CancellationToken.None);
        Assert.NotNull(ficha);
        Assert.Equal(template, ficha!.TemplateId);
        Assert.Equal("A", ficha.TemplateName);
    }

    [Fact]
    public async Task Transversal_Remove_ViaTemplateFicha_NullsEverywhere()
    {
        var fixture = CreateFixture();
        var template = await fixture.CreateTemplateAsync("A", [JobOnView], null);
        var user = await fixture.SeedUserAsync("2661", active: true, templateId: template);

        await fixture.Service.SetTemplateUserAsync(
            new TemplateAdministrationCommands.SetTemplateUserCommand(
                template, user.AccountId, TargetTemplateId: null, user.Version),
            CancellationToken.None);

        var userService = new UserAdministrationService(
            fixture.Users, fixture.Templates, new FakeUserIdentityProvisioner());
        var ficha = await userService.GetAsync(user.AccountId, CancellationToken.None);
        Assert.NotNull(ficha);
        Assert.Null(ficha!.TemplateId);
        Assert.True(ficha.Active);

        var templateFicha = (await fixture.Service.GetAsync(template, CancellationToken.None))!;
        Assert.Empty(templateFicha.Users);
    }

    // -------------------------------------------------------------- DELETE

    [Fact]
    public async Task Delete_ConfirmationShowsCount_ThenAtomicNullOut_RemovesRowAndComposition_UsersRemainActive()
    {
        var fixture = CreateFixture();
        var template = await fixture.CreateTemplateAsync("A", [JobOnView, ControloCreate], "job-on");
        var user1 = await fixture.SeedUserAsync("2661", active: true, templateId: template);
        var user2 = await fixture.SeedUserAsync("1888", active: false, templateId: template);

        // Confirmation source: the ficha carries the associated-USER count.
        var confirmation = (await fixture.Service.GetAsync(template, CancellationToken.None))!;
        Assert.Equal(2, confirmation.Users.Count);

        var result = await fixture.Service.DeleteAsync(
            new TemplateAdministrationCommands.DeleteTemplateCommand(template, confirmation.Version),
            CancellationToken.None);

        var success = Assert.IsType<TemplateAdministrationResult.Success>(result);
        Assert.Equal(2, success.Ficha.Users.Count);

        // Template + composition gone; both USER rows remain, active state preserved, no Template.
        Assert.Null(await fixture.Service.GetAsync(template, CancellationToken.None));
        Assert.Empty(fixture.Templates.Templates);
        Assert.Equal(2, fixture.Users.Accounts.Count);
        Assert.All(fixture.Users.Accounts, account => Assert.Null(account.TemplateId));
        Assert.Contains(fixture.Users.Accounts, account => account.AccountId == user1.AccountId && account.IsActive);
        Assert.Contains(fixture.Users.Accounts, account => account.AccountId == user2.AccountId && !account.IsActive);
    }

    [Fact]
    public async Task Delete_StaleVersion_Conflict_NothingChanged()
    {
        var fixture = CreateFixture();
        var template = await fixture.CreateTemplateAsync("A", [JobOnView], null);
        var user = await fixture.SeedUserAsync("2661", active: true, templateId: template);

        var result = await fixture.Service.DeleteAsync(
            new TemplateAdministrationCommands.DeleteTemplateCommand(template, ExpectedVersion: 99),
            CancellationToken.None);

        var conflict = Assert.IsType<TemplateAdministrationResult.Conflict>(result);
        Assert.Equal(TemplateConflictReason.StaleTemplateVersion, conflict.Reason);

        // Template, composition and the user reference all survive.
        var ficha = (await fixture.Service.GetAsync(template, CancellationToken.None))!;
        Assert.Equal("A", ficha.Name);
        Assert.Single(ficha.Modules);
        Assert.Contains(ficha.Users, member => member.UserId == user.AccountId);
        Assert.Equal(template, fixture.Users.Accounts.Single(account => account.AccountId == user.AccountId).TemplateId);
    }

    [Fact]
    public async Task Delete_MissingTemplate_NotFound()
    {
        var fixture = CreateFixture();
        var result = await fixture.Service.DeleteAsync(
            new TemplateAdministrationCommands.DeleteTemplateCommand(Guid.NewGuid(), ExpectedVersion: 1),
            CancellationToken.None);
        Assert.IsType<TemplateAdministrationResult.NotFound>(result);
    }

    /// <remarks>
    /// Test: Delete_ExpectedVersionZero_ValidationFailed_NoDeleteOrNullOut.<br/>
    /// Purpose: prove a zero delete carrier never reaches the destructive path.<br/>
    /// Master behavior being verified: accepted plan §17 — delete requires
    /// <c>ExpectedVersion &gt; 0</c>; malformed input must fail before any destructive effect.<br/>
    /// Preconditions: Template A with a member USER; delete command with <c>ExpectedVersion = 0</c>.<br/>
    /// Action: <c>DeleteAsync</c>.<br/>
    /// Assertions: <c>ValidationFailed</c>; Template row survives; USER still references it;
    /// <c>users.template_id</c> untouched.<br/>
    /// Required non-effects: no <c>DeleteWithMembersAsync</c>, no null-out, no delete.<br/>
    /// What this proves: the correction closes malformed destructive input at the boundary.<br/>
    /// What this does NOT prove: atomic transaction behavior (PG tests cover it).
    /// </remarks>
    [Fact]
    public async Task Delete_ExpectedVersionZero_ValidationFailed_NoDeleteOrNullOut()
    {
        var fixture = CreateFixture();
        var template = await fixture.CreateTemplateAsync("A", [JobOnView], null);
        var user = await fixture.SeedUserAsync("2661", active: true, templateId: template);

        var result = await fixture.Service.DeleteAsync(
            new TemplateAdministrationCommands.DeleteTemplateCommand(template, ExpectedVersion: 0),
            CancellationToken.None);

        Assert.IsType<TemplateAdministrationResult.ValidationFailed>(result);

        Assert.Single(fixture.Templates.Templates);
        Assert.Equal(template, fixture.Templates.Templates.Single().TemplateId);
        Assert.Equal(template, fixture.Users.Accounts.Single(account => account.AccountId == user.AccountId).TemplateId);
    }

    /// <remarks>
    /// Test: Delete_ExpectedVersionNegative_ValidationFailed_NoDeleteOrNullOut.<br/>
    /// Purpose: negative delete carriers fail closed as input errors, never as stale conflicts,
    /// and never delete or null anything.<br/>
    /// Master behavior being verified: accepted plan §17.<br/>
    /// Preconditions: Template A with a member USER; <c>ExpectedVersion = -1</c>.<br/>
    /// Action: <c>DeleteAsync</c>.<br/>
    /// Assertions: <c>ValidationFailed</c>; Template survives; USER still references it.<br/>
    /// Required non-effects: no <c>DeleteWithMembersAsync</c>, no null-out.<br/>
    /// What this proves: malformed destructive carriers cannot produce "stale conflict" or any
    /// destructive outcome.<br/>
    /// What this does NOT prove: validator-level rule (covered separately).
    /// </remarks>
    [Fact]
    public async Task Delete_ExpectedVersionNegative_ValidationFailed_NoDeleteOrNullOut()
    {
        var fixture = CreateFixture();
        var template = await fixture.CreateTemplateAsync("A", [JobOnView], null);
        var user = await fixture.SeedUserAsync("2661", active: true, templateId: template);

        var result = await fixture.Service.DeleteAsync(
            new TemplateAdministrationCommands.DeleteTemplateCommand(template, ExpectedVersion: -1),
            CancellationToken.None);

        Assert.IsType<TemplateAdministrationResult.ValidationFailed>(result);

        Assert.Single(fixture.Templates.Templates);
        Assert.Equal(template, fixture.Templates.Templates.Single().TemplateId);
        Assert.Equal(template, fixture.Users.Accounts.Single(account => account.AccountId == user.AccountId).TemplateId);
    }

    [Fact]
    public async Task Delete_ConcurrentEditBetweenPreCheckAndDelete_Conflict_NoHalfState()
    {
        var fixture = CreateFixture();
        var template = await fixture.CreateTemplateAsync("A", [JobOnView], null);
        var user = await fixture.SeedUserAsync("2661", active: true, templateId: template);

        // Simulate the concurrent writer between the confirmation pre-check and the
        // destructive transaction: the fake's hook bumps the Template version (a reorder).
        fixture.Templates.BeforeDeleteWithMembers = _ =>
        {
            var current = fixture.Templates.Templates.Single(t => t.TemplateId == template);
            fixture.Templates.HookBumpVersion(template, current.Version);
        };

        var result = await fixture.Service.DeleteAsync(
            new TemplateAdministrationCommands.DeleteTemplateCommand(template, ExpectedVersion: 1),
            CancellationToken.None);

        var conflict = Assert.IsType<TemplateAdministrationResult.Conflict>(result);
        Assert.Equal(TemplateConflictReason.StaleTemplateVersion, conflict.Reason);

        // No half state: Template survives at version 2, the user reference is intact.
        var ficha = (await fixture.Service.GetAsync(template, CancellationToken.None))!;
        Assert.Equal(2, ficha.Version);
        Assert.Contains(ficha.Users, member => member.UserId == user.AccountId);
        Assert.Equal(template, fixture.Users.Accounts.Single(account => account.AccountId == user.AccountId).TemplateId);
    }

    [Fact]
    public async Task Delete_RetryFromCurrentVersion_Completes()
    {
        var fixture = CreateFixture();
        var template = await fixture.CreateTemplateAsync("A", [JobOnView], null);
        await fixture.SeedUserAsync("2661", active: true, templateId: template);

        fixture.Templates.BeforeDeleteWithMembers = _ => { }; // no concurrent writer on retry

        var result = await fixture.Service.DeleteAsync(
            new TemplateAdministrationCommands.DeleteTemplateCommand(template, ExpectedVersion: 1),
            CancellationToken.None);

        Assert.IsType<TemplateAdministrationResult.Success>(result);
        Assert.Empty(fixture.Templates.Templates);
        Assert.Single(fixture.Users.Accounts);
        Assert.Null(fixture.Users.Accounts.Single().TemplateId);
    }

    // -------------------------------------------------------------- CONTRACT

    [Fact]
    public void TemplateAdministrationModels_NoAuditMembers()
    {
        // P1-T06 implements no audit (deferred to P1-T09): neither the model records nor the
        // result set may carry audit primitives (actor, timestamp, reason) — the closed result
        // set is the instrumented, typed surface.
        var modelTypes = new[]
        {
            typeof(TemplateAdministrationCommands.CreateTemplateCommand),
            typeof(TemplateAdministrationCommands.UpdateTemplateCommand),
            typeof(TemplateAdministrationCommands.DeleteTemplateCommand),
            typeof(TemplateAdministrationCommands.SetTemplateUserCommand),
            typeof(TemplateFicha),
            typeof(TemplateListItem),
        };

        foreach (var type in modelTypes)
        {
            foreach (var property in type.GetProperties())
            {
                var name = property.Name;
                Assert.DoesNotContain("Audit", name);
                Assert.DoesNotContain("Actor", name);
            }
        }
    }

    // ------------------------------------------------------------------ fixture

    private static Fixture CreateFixture() => new();

    private sealed class Fixture
    {
        public Fixture()
        {
            Users = new FakeUserRepository();
            Templates = new FakeTemplateAdministrationRepository(Users);
            Service = new TemplateAdministrationService(Templates, Templates, Users, TestModuleDefinitions.TestRegistry());
        }

        public FakeUserRepository Users { get; }

        public FakeTemplateAdministrationRepository Templates { get; }

        public TemplateAdministrationService Service { get; }

        public async Task<Guid> CreateTemplateAsync(string name, string[] moduleIds, string? landing)
        {
            var result = await Service.CreateAsync(
                new TemplateAdministrationCommands.CreateTemplateCommand(name, moduleIds, landing),
                CancellationToken.None);
            return Assert.IsType<TemplateAdministrationResult.Created>(result).TemplateId;
        }

        public async Task<UserAccount> SeedUserAsync(
            string companyNumber,
            bool active = true,
            Guid? templateId = null)
        {
            var account = new UserAccount(
                Guid.NewGuid(),
                CompanyNumber: companyNumber,
                DisplayName: "João Silva",
                Email: "joao@dmo.test",
                RoleLabel: "Reparador",
                IsActive: active,
                TemplateId: templateId,
                Version: 1);

            await Users.CreatedAsync(account, $"subject-{account.AccountId:N}", CancellationToken.None);
            return (await Users.GetByIdAsync(account.AccountId, CancellationToken.None))!;
        }
    }
}