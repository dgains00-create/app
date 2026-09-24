using DMO.Application.Boquilhas;
using DMO.Domain.Boquilhas;
using DMO.Domain.Tools;

namespace DMO.UnitTests.Boquilhas;

/// <summary>
/// P2-T07 unit rows for the pure validator (contract §9.2/§12.2): the closed §12.2 code set, the
/// movement-type refusal (V3), the anchor rules, the opening-facts rules and the query filters.
/// </summary>
public sealed class BoquilhasValidatorTests
{
    // ------------------------------------------------------------------ movement vocabulary (V3)

    /// <summary>
    /// V3 (AC-V3) — the append validator refuses any type outside the closed set with exactly
    /// <c>MOVEMENT_TYPE_INVALID</c>; <c>editar</c> is never a valid type.
    /// </summary>
    [Theory]
    [InlineData("editar")]
    [InlineData("contagem")]
    [InlineData("")]
    [InlineData("saída")]
    public void V3_AnInvalidMovementTypeIsRefusedWithMovementTypeInvalid(string movementType)
    {
        var errors = BoquilhasValidator.Validate(new AppendMovementCommand(
            Guid.NewGuid(),
            ExpectedAggregateVersion: 1,
            movementType,
            Quantity: 1,
            DateOnly.FromDateTime(DateTime.Today),
            Machine: null,
            RepairerId: null,
            Observations: null));

        Assert.Contains(BoquilhasValidationErrors.MovementTypeInvalid, errors);
    }

    /// <summary>The four closed tokens are all accepted by the movement validator.</summary>
    [Theory]
    [InlineData("inicio")]
    [InlineData("saida")]
    [InlineData("entrada")]
    [InlineData("irreparavel")]
    public void V3_TheFourClosedTokensAreAccepted(string movementType)
    {
        var errors = BoquilhasValidator.Validate(new AppendMovementCommand(
            Guid.NewGuid(),
            ExpectedAggregateVersion: 1,
            movementType,
            Quantity: 1,
            DateOnly.FromDateTime(DateTime.Today),
            Machine: null,
            RepairerId: null,
            Observations: null));

        Assert.DoesNotContain(BoquilhasValidationErrors.MovementTypeInvalid, errors);
    }

    // ------------------------------------------------------------------ quantities (AC-B3/B4)

    /// <summary>A non-positive quantity is refused with the exact token.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void QuantityNotPositiveIsRefused(int quantity)
    {
        var errors = BoquilhasValidator.Validate(new AppendMovementCommand(
            Guid.NewGuid(), 1, "saida", quantity,
            DateOnly.FromDateTime(DateTime.Today), "B1", Guid.NewGuid(), null));

        Assert.Contains(BoquilhasValidationErrors.QuantityNotPositive, errors);
    }

    // ------------------------------------------------------------------ anchors (AC-I7)

    /// <summary>Neither anchor supplied → ANCHOR_REQUIRED; both supplied → ANCHOR_CONFLICT.</summary>
    [Fact]
    public void AnchorRulesAreExact()
    {
        var baseCommand = new CreateBoquilhasCommand(
            BqId: null,
            ToolId: null,
            Machines: ["B1"],
            InitialQuantity: 5,
            DateOnly.FromDateTime(DateTime.Today),
            UtilisationPercent: null,
            Observations: null,
            CreatedByUserId: Guid.NewGuid());

        Assert.Contains(BoquilhasValidationErrors.AnchorRequired, BoquilhasValidator.Validate(baseCommand));

        var both = baseCommand with { BqId = Guid.NewGuid(), ToolId = Guid.NewGuid() };
        Assert.Contains(BoquilhasValidationErrors.AnchorConflict, BoquilhasValidator.Validate(both));

        var linked = baseCommand with { BqId = Guid.NewGuid() };
        Assert.DoesNotContain(BoquilhasValidationErrors.AnchorRequired, BoquilhasValidator.Validate(linked));

        var standalone = baseCommand with { ToolId = Guid.NewGuid() };
        Assert.DoesNotContain(BoquilhasValidationErrors.AnchorRequired, BoquilhasValidator.Validate(standalone));
    }

    // ------------------------------------------------------------------ opening facts (AC-I7/U1)

    /// <summary>Machines: at least one (MACHINES_REQUIRED); only settled codes (MACHINE_UNKNOWN);
    /// no duplicate entries (the same token, defensive-first discipline).</summary>
    [Fact]
    public void MachineRulesAreExact()
    {
        var command = new CreateBoquilhasCommand(
            BqId: null,
            ToolId: Guid.NewGuid(),
            Machines: [],
            InitialQuantity: 5,
            OpeningDate: DateOnly.FromDateTime(DateTime.Today),
            UtilisationPercent: null,
            Observations: null,
            CreatedByUserId: Guid.NewGuid());

        Assert.Contains(BoquilhasValidationErrors.MachinesRequired, BoquilhasValidator.Validate(command));

        var unknown = command with { Machines = ["B1", "X9"] };
        Assert.Contains(BoquilhasValidationErrors.MachineUnknown, BoquilhasValidator.Validate(unknown));

        var duplicate = command with { Machines = ["B1", "B1"] };
        Assert.Contains(BoquilhasValidationErrors.MachineUnknown, BoquilhasValidator.Validate(duplicate));
    }

    /// <summary>Initial quantity: zero → INITIAL_QUANTITY_REQUIRED; negative → QUANTITY_NOT_POSITIVE.</summary>
    [Theory]
    [InlineData(0, "INITIAL_QUANTITY_REQUIRED")]
    [InlineData(-1, "QUANTITY_NOT_POSITIVE")]
    public void InitialQuantityRulesAreExact(int quantity, string expectedToken)
    {
        var command = new CreateBoquilhasCommand(
            BqId: null,
            ToolId: Guid.NewGuid(),
            Machines: ["B1"],
            InitialQuantity: quantity,
            OpeningDate: DateOnly.FromDateTime(DateTime.Today),
            UtilisationPercent: null,
            Observations: null,
            CreatedByUserId: Guid.NewGuid());

        Assert.Contains(expectedToken, BoquilhasValidator.Validate(command));
    }

    /// <summary>The manual utilisation still is 0–100 or NULL (UTILISATION_INVALID otherwise).</summary>
    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void UtilisationRangeIsExact(decimal utilisation)
    {
        var command = new CreateBoquilhasCommand(
            BqId: null,
            ToolId: Guid.NewGuid(),
            Machines: ["B1"],
            InitialQuantity: 5,
            OpeningDate: DateOnly.FromDateTime(DateTime.Today),
            UtilisationPercent: utilisation,
            Observations: null,
            CreatedByUserId: Guid.NewGuid());

        Assert.Contains(BoquilhasValidationErrors.UtilisationInvalid, BoquilhasValidator.Validate(command));
    }

    // ------------------------------------------------------------------ reopen reason (C5)

    /// <summary>Reopen requires a non-blank reason (REOPEN_REASON_REQUIRED).</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ReopenReasonIsRequired(string reason)
    {
        var errors = BoquilhasValidator.Validate(new ReopenBoquilhasCommand(
            Guid.NewGuid(), ExpectedVersion: 1, reason));

        Assert.Contains(BoquilhasValidationErrors.ReopenReasonRequired, errors);
    }

    // ------------------------------------------------------------------ query filters (H1)

    /// <summary>The history filters refuse unknown/ill-formed values (FILTER_INVALID), never a
    /// silent full list.</summary>
    [Fact]
    public void HistoryFiltersRefuseUnknownValues()
    {
        var badState = new BoquilhasHistoryQuery(
            State: "rascunho", null, null, null, null, null, null, null, 1, 50);
        Assert.Contains(BoquilhasValidationErrors.FilterInvalid, BoquilhasValidator.Validate(badState));

        var badMachine = new BoquilhasHistoryQuery(
            null, null, null, "X9", null, null, null, null, 1, 50);
        Assert.Contains(BoquilhasValidationErrors.FilterInvalid, BoquilhasValidator.Validate(badMachine));

        var badType = new BoquilhasHistoryQuery(
            null, null, null, null, null, null, "editar", null, 1, 50);
        Assert.Contains(BoquilhasValidationErrors.FilterInvalid, BoquilhasValidator.Validate(badType));

        var invertedPeriod = new BoquilhasHistoryQuery(
            null, null, null, null,
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            null, null, 1, 50);
        Assert.Contains(BoquilhasValidationErrors.FilterInvalid, BoquilhasValidator.Validate(invertedPeriod));

        var badPaging = new BoquilhasHistoryQuery(
            null, null, null, null, null, null, null, null, Page: 0, PageSize: 101);
        Assert.Contains(BoquilhasValidationErrors.FilterInvalid, BoquilhasValidator.Validate(badPaging));
    }

    /// <summary>The valid history filter set is accepted untouched.</summary>
    [Fact]
    public void HistoryFiltersAcceptTheValidSet()
    {
        var valid = new BoquilhasHistoryQuery(
            State: "closed",
            Reference: "REF",
            Lot: "L1",
            Machine: "B1",
            DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            DateOnly.FromDateTime(DateTime.Today),
            "saida",
            Guid.NewGuid(),
            2,
            25);

        Assert.Empty(BoquilhasValidator.Validate(valid));
    }
}

/// <summary>Vocabulary/identity unit rows of the Boquilhas area (contract §3/§12/§15).</summary>
public sealed class BoquilhasContractTests
{
    /// <summary>
    /// V1 (AC-V1) — the closed §3.1 token set has EXACTLY four members and no
    /// <c>Editar</c>/legacy value exists; the tokens parse back to the enum only for those four.
    /// </summary>
    [Fact]
    public void V1_TheClosedTokenSetHasExactlyFourMembers()
    {
        Assert.Equal(
            new[] { MovementKind.Inicio, MovementKind.Saida, MovementKind.Entrada, MovementKind.Irreparavel },
            Enum.GetValues<MovementKind>());

        foreach (var token in new[] { "inicio", "saida", "entrada", "irreparavel" })
        {
            Assert.NotNull(MovementKindTokens.Parse(token));
        }

        Assert.Null(MovementKindTokens.Parse("editar"));
        Assert.Null(MovementKindTokens.Parse("contagem"));
        Assert.Null(MovementKindTokens.Parse(null));
    }

    /// <summary>
    /// V2 (AC-V2) — the parse/token mapping has no Editar member and no cancel/annul token:
    /// Editar is an ACTION, never a type — proven by the closed enum + tokens + refusal set.
    /// </summary>
    [Fact]
    public void V2_EditarIsNeverAType()
    {
        Assert.All(Enum.GetValues<MovementKind>(), kind =>
            Assert.DoesNotContain("editar", MovementKindTokens.ToToken(kind), StringComparison.Ordinal));
    }

    /// <summary>The status vocabulary is exactly active|closed (aggregate lifecycle, §3.2).</summary>
    [Fact]
    public void StatusVocabularyIsExactlyActiveAndClosed()
    {
        Assert.Equal(
            new[] { BoquilhaStatus.Active, BoquilhaStatus.Closed },
            Enum.GetValues<BoquilhaStatus>());

        Assert.Equal("active", BoquilhaStatusTokens.ToToken(BoquilhaStatus.Active));
        Assert.Equal("closed", BoquilhaStatusTokens.ToToken(BoquilhaStatus.Closed));
        Assert.Null(BoquilhaStatusTokens.Parse("arquivado"));
    }

    /// <summary>
    /// I4 (AC-I4 unit facet) — the carriage types carry no false identities: the commands hold
    /// only <c>boquilhas_id</c>/<c>movement_id</c> route identities, anchors and facts — no
    /// <c>production_id</c>, no <c>job_on_revision_id</c>, no client-minted id.
    /// </summary>
    [Fact]
    public void I4_CarriersCarryNoFalseIdentity()
    {
        var command = new AppendMovementCommand(
            Guid.NewGuid(), 1, "saida", 1,
            DateOnly.FromDateTime(DateTime.Today), "B1", Guid.NewGuid(), null);

        var members = command.GetType().GetProperties().Select(property => property.Name).ToList();
        Assert.DoesNotContain(members, member => member.Contains("Production", StringComparison.Ordinal));
        Assert.DoesNotContain(members, member => member.Contains("Revision", StringComparison.Ordinal));

        var create = new CreateBoquilhasCommand(
            null, Guid.NewGuid(), ["B1"], 1, DateOnly.FromDateTime(DateTime.Today), null, null, Guid.NewGuid());
        var createMembers = create.GetType().GetProperties().Select(property => property.Name).ToList();
        Assert.DoesNotContain(createMembers, member => member.Contains("JobOn", StringComparison.Ordinal));
        Assert.DoesNotContain(createMembers, member => member.Contains("Production", StringComparison.Ordinal));
    }

    /// <summary>
    /// E5 (AC-E5 unit facet) — the edit carrier accepts no actor/time: the edit command and its
    /// transport request expose only the editable values + versions.
    /// </summary>
    [Fact]
    public void E5_TheEditCarrierAcceptsNoActorOrTime()
    {
        var command = new EditMovementCommand(
            Guid.NewGuid(), 1, Guid.NewGuid(), 1, 5,
            DateOnly.FromDateTime(DateTime.Today), "B1", null, "nota");

        var members = command.GetType().GetProperties().Select(property => property.Name).ToList();
        Assert.DoesNotContain(members, member => member.Contains("User", StringComparison.Ordinal));
        Assert.DoesNotContain(members, member => member.Contains("Actor", StringComparison.Ordinal));
        Assert.DoesNotContain(members, member => member.Contains("At", StringComparison.Ordinal) || member.Contains("Time", StringComparison.Ordinal));
        Assert.DoesNotContain(members, member => member.Contains("RecordedAt", StringComparison.Ordinal));
        Assert.DoesNotContain(members, member => member.Contains("MovementType", StringComparison.Ordinal));
    }

    /// <summary>
    /// E6 (AC-E6 unit facet) — movement_type and recorded_at are immutable by carrier shape: they
    /// appear in no edit carrier member.
    /// </summary>
    [Fact]
    public void E6_ImmutablesAreAbsentFromTheEditCarrier()
    {
        var members = typeof(EditMovementCommand).GetProperties().Select(property => property.Name).ToList();
        Assert.DoesNotContain(members, member => member == "MovementType");
        Assert.DoesNotContain(members, member => member == "RecordedAt");
    }

    /// <summary>
    /// A1 (AC-A1 unit facet) — the transport refusal tokens of §12.3 are closed and exact: every
    /// BoquilhasRefusalReason maps to exactly one token and no token is shared.
    /// </summary>
    [Fact]
    public void A1_RefusalTokensAreClosedAndExact()
    {
        var expected = new Dictionary<BoquilhasRefusalReason, string>
        {
            [BoquilhasRefusalReason.StaleVersion] = "stale-version",
            [BoquilhasRefusalReason.SaidaExceedsAvailable] = "saida-exceeds-available",
            [BoquilhasRefusalReason.IrreparavelExceedsInRepair] = "irreparavel-exceeds-in-repair",
            [BoquilhasRefusalReason.ActiveAggregateExists] = "active-aggregate-exists",
            [BoquilhasRefusalReason.AlreadyClosed] = "already-closed",
            [BoquilhasRefusalReason.NotClosed] = "not-closed",
            [BoquilhasRefusalReason.NotLastClosed] = "not-last-closed",
            [BoquilhasRefusalReason.AggregateClosed] = "aggregate-closed",
            [BoquilhasRefusalReason.OnlyOneInicio] = "only-one-inicio",
        };

        Assert.Equal(expected.Keys.OrderBy(key => key.ToString()), Enum.GetValues<BoquilhasRefusalReason>().OrderBy(key => key.ToString()));

        foreach (var (reason, token) in expected)
        {
            Assert.Equal(token, DMO.Web.Endpoints.BoquilhasEndpoints.RefusalToken(reason));
        }

        Assert.Equal(expected.Count, expected.Values.Distinct().Count());
    }
}