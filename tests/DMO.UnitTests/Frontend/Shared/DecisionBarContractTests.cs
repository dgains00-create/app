using DMO.Web.Frontend.Shared.Contracts;

namespace DMO.UnitTests.Frontend.Shared;

/// <summary>
/// P2-T03 (A6) unit — the action-region carrier and the deterministic invocation model.
/// Authority: <c>plans/contracts/P2-T03_TOOLPICKER_ROWS_DECISIONBAR_CONTRACT.md</c> §3.4, §4.3 and
/// the matrix rows DB1–DB11 (AC-35 … AC-42, AC-45, AC-46).
/// Purpose: prove the frozen action-region rules as failing-if-removed unit evidence — the supplied
/// order is authoritative and the supplied classification never reorders or filters, the accepted
/// disabled-reason rule is inherited rather than re-implemented, a pending action is presented busy
/// with its supplied label and cannot be invoked again until the consumer releases it, nothing else
/// suppresses an invocation, and no transition semantics are encoded anywhere.
/// Preconditions: none; the carriers are deterministic presentation-only types.
/// Required non-effects: no action is disabled because of the region state, and no member, constant,
/// group or default label names a domain transition.
/// </summary>
public sealed class DecisionBarContractTests
{
    [Fact]
    public void DB1_SuppliedOrderIsPreserved_ForEveryGroupInterleaving()
    {
        var presentation = Bar(
            CommonState.Ready,
            Action("a", DecisionBarActionGroup.Primary),
            Action("b", DecisionBarActionGroup.Secondary),
            Action("c", DecisionBarActionGroup.Primary),
            Action("d", DecisionBarActionGroup.Danger));

        Assert.Equal(["a", "b", "c", "d"], presentation.VisibleActions.Select(action => action.Key));
        Assert.Equal(
            ["primary", "secondary", "primary", "danger"],
            presentation.VisibleActions.Select(action => action.GroupToken));
    }

    [Fact]
    public void DB2_GroupAssignmentIsPreservedAndMandatory()
    {
        var presentation = Bar(
            CommonState.Ready,
            Action("a", DecisionBarActionGroup.Danger),
            Action("b", DecisionBarActionGroup.Primary));

        Assert.Equal(DecisionBarActionGroup.Danger, presentation.VisibleActions[0].Group);
        Assert.Equal("danger", presentation.VisibleActions[0].GroupToken);
        Assert.True(presentation.VisibleActions[0].RequiresTextualGroupMeaning);

        Assert.Equal(DecisionBarActionGroup.Primary, presentation.VisibleActions[1].Group);
        Assert.Equal("primary", presentation.VisibleActions[1].GroupToken);
        Assert.False(presentation.VisibleActions[1].RequiresTextualGroupMeaning);

        // Only the three supplied classifications are representable, and the group is mandatory.
        Assert.Equal(3, Enum.GetNames<DecisionBarActionGroup>().Length);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DecisionBarActionPresentation.Create(
                SharedActionPresentation.CreateEnabled("a", "Ação"), (DecisionBarActionGroup)99));
    }

    [Fact]
    public void DB3_DisabledReasonIsInherited_AndNeverInvented()
    {
        var disabled = SharedActionPresentation.CreateDisabled("a", "Ação", "Motivo fornecido.");
        var presentation = Bar(CommonState.Ready, DecisionBarActionPresentation.Create(disabled, DecisionBarActionGroup.Secondary));

        Assert.True(presentation.IsActionUnavailable(presentation.VisibleActions[0]));
        Assert.Equal("Motivo fornecido.", presentation.UnavailableReason(presentation.VisibleActions[0]));

        // The accepted carrier still enforces the reason, and the action key/label stay mandatory.
        Assert.ThrowsAny<ArgumentException>(() =>
            SharedActionPresentation.Create("a", "Ação", enabled: false));

        Assert.ThrowsAny<ArgumentException>(() =>
            SharedActionPresentation.Create(" ", "Ação", enabled: true));

        Assert.ThrowsAny<ArgumentException>(() =>
            SharedActionPresentation.Create("a", " ", enabled: true));
    }

    [Fact]
    public void DB4_PendingState_ExposesTheSuppliedLabel_AndKeepsTheAccessibleName()
    {
        var pending = SharedActionPresentation.Create("a", "Ação fornecida", true, null, "Em curso…");
        var presentation = Bar(
            CommonState.Ready,
            DecisionBarActionPresentation.Create(pending, DecisionBarActionGroup.Primary),
            pendingActionKey: "a");

        var action = presentation.VisibleActions[0];

        Assert.True(presentation.HasPendingAction);
        Assert.True(presentation.IsRegionBusy);
        Assert.True(presentation.IsActionPending(action));
        Assert.True(presentation.ShowsPendingText(action));
        Assert.True(presentation.IsActionUnavailable(action));

        // The accessible name stays the supplied label; the pending label is supplementary text.
        Assert.Equal("Ação fornecida", action.Label);
        Assert.Equal("Em curso…", action.Action.PendingLabel);
    }

    [Fact]
    public void DB5_DuplicateInvocationIsBlockedWhilePending_AndReleasedByCompletion()
    {
        var interaction = new DecisionBarInteraction("a");

        var first = interaction.Invoke("a");
        var second = interaction.Invoke("a");

        Assert.True(first.DuplicateBlocked);
        Assert.False(first.HasEvent);
        Assert.True(second.DuplicateBlocked);
        Assert.False(second.HasEvent);

        // Completion: the consumer adopts a new controlled pending value.
        interaction.Reset(null);

        var released = interaction.Invoke("a");

        Assert.False(released.DuplicateBlocked);
        Assert.Equal(DecisionBarEventKind.ActionInvoked, released.Event!.Kind);
        Assert.Equal("a", released.Event.ActionKey);

        interaction.Sync("a");

        Assert.True(interaction.Invoke("a").DuplicateBlocked);
    }

    [Fact]
    public void DB6_AnotherActionIsNotBlockedByAPendingAction()
    {
        var interaction = new DecisionBarInteraction("a");

        var other = interaction.Invoke("b");

        Assert.False(other.DuplicateBlocked);
        Assert.Equal("b", other.Event!.ActionKey);
        Assert.Equal("a", interaction.PendingActionKey);
        Assert.True(interaction.Invoke("a").DuplicateBlocked);
    }

    [Fact]
    public void DB7_NoOtherSuppressionExists_ForANonPendingAction()
    {
        var interaction = new DecisionBarInteraction();

        var first = interaction.Invoke("a");
        var second = interaction.Invoke("a");

        Assert.True(first.HasEvent);
        Assert.True(second.HasEvent);
        Assert.False(second.DuplicateBlocked);
        Assert.Null(interaction.PendingActionKey);
    }

    [Fact]
    public void DB8_NoTransitionOrDomainSemanticsAreEncoded()
    {
        string[] forbidden =
        [
            "approve", "reject", "reopen", "submit", "close", "movement", "release", "repair",
            "warehouse", "armaz", "irreparável", "início", "saída", "entrada",
        ];

        var names = P2T03TypeScan.MemberNames(P2T03TypeScan.DecisionTypes)
            .Concat(P2T03TypeScan.ConstantStrings(P2T03TypeScan.DecisionTypes))
            .Concat(P2T03TypeScan.EnumMemberNames(P2T03TypeScan.DecisionTypes));

        foreach (var name in names)
        {
            foreach (var token in forbidden)
            {
                Assert.DoesNotContain(token, name, StringComparison.OrdinalIgnoreCase);
            }
        }

        // The only supplied classifications remain the three presentation groups.
        Assert.Equal(["Primary", "Secondary", "Danger"], Enum.GetNames<DecisionBarActionGroup>());
    }

    [Fact]
    public void DB9_AvailabilityIsExactlySupplied_RegardlessOfRegionState()
    {
        var action = Action("a", DecisionBarActionGroup.Primary);

        foreach (var state in new[]
                 {
                     CommonState.Ready, CommonState.Stale, CommonState.Conflict,
                     CommonState.Unavailable, CommonState.PermissionDenied,
                     CommonState.Saving, CommonState.Submitting, CommonState.Loading,
                     CommonState.Empty, CommonState.LookupFailed,
                 })
        {
            var presentation = Bar(state, action);
            var supplied = presentation.VisibleActions[0];

            Assert.False(
                presentation.IsActionUnavailable(supplied),
                $"The region must not disable a supplied enabled action in {state}.");

            Assert.True(new DecisionBarInteraction().Invoke(supplied.Key).HasEvent);
        }

        // A pending action is the one frozen exception.
        var pending = Bar(CommonState.Stale, action, pendingActionKey: "a");

        Assert.True(pending.IsActionUnavailable(pending.VisibleActions[0]));
        Assert.True(new DecisionBarInteraction("a").Invoke("a").DuplicateBlocked);
    }

    [Fact]
    public void DB10_Create_FailsClosedOnSuppliedInconsistencies()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DecisionBarPresentation.Create((CommonState)99));

        Assert.ThrowsAny<ArgumentException>(() =>
            DecisionBarPresentation.Create(
                CommonState.Ready,
                [Action("a", DecisionBarActionGroup.Primary), Action("a", DecisionBarActionGroup.Danger)]));

        Assert.ThrowsAny<ArgumentException>(() =>
            DecisionBarPresentation.Create(CommonState.Stale));

        Assert.ThrowsAny<ArgumentException>(() =>
            DecisionBarPresentation.Create(CommonState.Ready, [Action("a", DecisionBarActionGroup.Primary)], pendingActionKey: "b"));

        Assert.Throws<ArgumentNullException>(() =>
            DecisionBarActionPresentation.Create(null!, DecisionBarActionGroup.Primary));
    }

    [Fact]
    public void DB11_DecisionTypes_AreDomainNeutral_WithNoAddressOrCanonicalIdentityName()
    {
        foreach (var type in P2T03TypeScan.DecisionTypes)
        {
            Assert.Equal(P2T03TypeScan.ContractsNamespace, type.Namespace);
        }

        var memberTypes = P2T03TypeScan.MemberTypes(P2T03TypeScan.DecisionTypes).ToList();

        foreach (var marker in new[]
                 {
                     "Service", "DbContext", "HttpClient", "Supabase", "Session", "Clock",
                     "DMO.Application", "DMO.Domain", "DMO.Infrastructure", "Uri", "Route",
                 })
        {
            Assert.DoesNotContain(
                memberTypes,
                type => (type.FullName ?? type.Name).Contains(marker, StringComparison.Ordinal));
        }

        var names = P2T03TypeScan.MemberNames(P2T03TypeScan.DecisionTypes)
            .Concat(P2T03TypeScan.ConstantStrings(P2T03TypeScan.DecisionTypes))
            .Concat(P2T03TypeScan.EnumMemberNames(P2T03TypeScan.DecisionTypes));

        foreach (var name in names)
        {
            foreach (var token in P2T03TypeScan.CanonicalIdentityTokens)
            {
                Assert.DoesNotContain(token, name, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private static DecisionBarActionPresentation Action(string key, DecisionBarActionGroup group) =>
        DecisionBarActionPresentation.Create(
            SharedActionPresentation.CreateEnabled(key, $"Ação {key}"), group);

    private static DecisionBarPresentation Bar(
        CommonState state,
        params DecisionBarActionPresentation[] actions) =>
        Create(state, actions, pendingActionKey: null);

    private static DecisionBarPresentation Bar(
        CommonState state,
        DecisionBarActionPresentation action,
        string? pendingActionKey) =>
        Create(state, [action], pendingActionKey);

    private static DecisionBarPresentation Create(
        CommonState state,
        IReadOnlyList<DecisionBarActionPresentation> actions,
        string? pendingActionKey) =>
        DecisionBarPresentation.Create(
            state,
            actions,
            pendingActionKey: pendingActionKey,
            message: state == CommonState.Ready ? null : "Mensagem fornecida.");
}
