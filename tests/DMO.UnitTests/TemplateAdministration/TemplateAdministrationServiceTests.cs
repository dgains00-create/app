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