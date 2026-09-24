using DMO.Application.Boquilhas;

namespace DMO.UnitTests.Boquilhas;

/// <summary>
/// The pure static validator of the OWNER CLARIFICATION register model: the closed three-type
/// vocabulary, the positive-quantity rule, business-date validity, the Saída required facts, the
/// machine code and the history filter conventions.
/// </summary>
public sealed class BoquilhasValidatorTests
{
    // ---- append: the closed vocabulary ------------------------------------------------------

    [Theory]
    [InlineData("saida")]
    [InlineData("entrada")]
    [InlineData("entrada_sem_reparacao")]
    public void Append_ThreeClosedTypes_AreValid(string movementType)
    {
        var command = NewAppend(movementType, quantity: 1);

        Assert.DoesNotContain(BoquilhasValidationErrors.MovementTypeInvalid, BoquilhasValidator.Validate(command));
    }

    [Theory]
    [InlineData("inicio")]
    [InlineData("irreparavel")]
    [InlineData("editar")]
    [InlineData("")]
    [InlineData("SAIDA")]
    public void Append_UnknownTypes_AreRefused(string movementType)
    {
        var command = NewAppend(movementType, quantity: 1);

        Assert.Contains(BoquilhasValidationErrors.MovementTypeInvalid, BoquilhasValidator.Validate(command));
    }

    // ---- append: quantity / date -----------------------------------------------------------

    [Fact]
    public void Append_ZeroQuantity_IsRefused()
    {
        var command = NewAppend("entrada", quantity: 0);

        Assert.Contains(BoquilhasValidationErrors.QuantityNotPositive, BoquilhasValidator.Validate(command));
    }

    [Fact]
    public void Append_NegativeQuantity_IsRefused()
    {
        var command = NewAppend("saida", quantity: -3);

        Assert.Contains(BoquilhasValidationErrors.QuantityNotPositive, BoquilhasValidator.Validate(command));
    }

    [Fact]
    public void Append_DefaultBusinessDate_IsRefused()
    {
        var command = NewAppend("entrada", quantity: 1, businessDate: new DateOnly(1, 1, 1));

        Assert.Contains(BoquilhasValidationErrors.BusinessDateInvalid, BoquilhasValidator.Validate(command));
    }

    /// <summary>A business date AFTER the production end date is a valid movement date (the
    /// production remains the historical context; nothing rejects a later date).</summary>
    [Fact]
    public void Append_LateBusinessDate_IsValid()
    {
        var command = NewAppend("entrada_sem_reparacao", quantity: 1, businessDate: new DateOnly(2026, 12, 31));

        Assert.DoesNotContain(BoquilhasValidationErrors.BusinessDateInvalid, BoquilhasValidator.Validate(command));
    }

    // ---- append: Saída required facts -------------------------------------------------------

    [Fact]
    public void Saida_WithoutMachine_IsRefused()
    {
        var command = NewAppend("saida", quantity: 1, machine: null, repairerId: Guid.NewGuid());

        Assert.Contains(BoquilhasValidationErrors.MachineRequired, BoquilhasValidator.Validate(command));
    }

    [Fact]
    public void Saida_WithoutRepairer_IsRefused()
    {
        var command = NewAppend("saida", quantity: 1, machine: "B1", repairerId: null);

        Assert.Contains(BoquilhasValidationErrors.RepairerRequired, BoquilhasValidator.Validate(command));
    }

    [Fact]
    public void Saida_WithMachineAndRepairer_IsValid()
    {
        var command = NewAppend("saida", quantity: 1, machine: "B1", repairerId: Guid.NewGuid());

        var errors = BoquilhasValidator.Validate(command);

        Assert.DoesNotContain(BoquilhasValidationErrors.MachineRequired, errors);
        Assert.DoesNotContain(BoquilhasValidationErrors.RepairerRequired, errors);
    }

    [Fact]
    public void Entrada_Variants_DoNotRequireMachineOrRepairer()
    {
        var entrada = NewAppend("entrada", quantity: 1, machine: null, repairerId: null);
        var semReparacao = NewAppend("entrada_sem_reparacao", quantity: 1, machine: null, repairerId: null);

        var entradaErrors = BoquilhasValidator.Validate(entrada);
        var semReparacaoErrors = BoquilhasValidator.Validate(semReparacao);

        Assert.DoesNotContain(BoquilhasValidationErrors.MachineRequired, entradaErrors);
        Assert.DoesNotContain(BoquilhasValidationErrors.RepairerRequired, entradaErrors);
        Assert.DoesNotContain(BoquilhasValidationErrors.MachineRequired, semReparacaoErrors);
        Assert.DoesNotContain(BoquilhasValidationErrors.RepairerRequired, semReparacaoErrors);
    }

    // ---- append: machine code ---------------------------------------------------------------

    [Fact]
    public void UnknownMachine_IsRefused()
    {
        var command = NewAppend("saida", quantity: 1, machine: "X9", repairerId: Guid.NewGuid());

        Assert.Contains(BoquilhasValidationErrors.MachineUnknown, BoquilhasValidator.Validate(command));
    }

    [Theory]
    [InlineData("B1")]
    [InlineData("B2")]
    [InlineData("B3")]
    [InlineData("C1")]
    [InlineData("C2")]
    [InlineData("C3")]
    public void SixOperationalMachines_AreKnown(string machine)
    {
        var command = NewAppend("saida", quantity: 1, machine: machine, repairerId: Guid.NewGuid());

        Assert.DoesNotContain(BoquilhasValidationErrors.MachineUnknown, BoquilhasValidator.Validate(command));
    }

    [Fact]
    public void EmptyRepairerId_IsRefused()
    {
        var command = NewAppend("saida", quantity: 1, machine: "B1", repairerId: Guid.Empty);

        Assert.Contains(BoquilhasValidationErrors.RepairerNotFound, BoquilhasValidator.Validate(command));
    }

    [Fact]
    public void BlankObservations_AreRefused()
    {
        var command = NewAppend("entrada", quantity: 1, observations: "   ");

        Assert.Contains(BoquilhasValidationErrors.ObservationsInvalid, BoquilhasValidator.Validate(command));
    }

    // ---- edit -------------------------------------------------------------------------------

    [Fact]
    public void Edit_ValidatesCoreShapesOnly()
    {
        var command = new EditMovementCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExpectedMovementVersion: 1,
            Quantity: 2,
            BusinessDate: new DateOnly(2026, 9, 27),
            Machine: "B1",
            RepairerId: Guid.NewGuid(),
            Observations: null);

        Assert.Empty(BoquilhasValidator.Validate(command));
    }

    [Fact]
    public void Edit_ZeroQuantity_IsRefused()
    {
        var command = new EditMovementCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExpectedMovementVersion: 1,
            Quantity: 0,
            BusinessDate: new DateOnly(2026, 9, 27),
            Machine: null,
            RepairerId: null,
            Observations: null);

        Assert.Contains(BoquilhasValidationErrors.QuantityNotPositive, BoquilhasValidator.Validate(command));
    }

    // ---- register creation ------------------------------------------------------------------

    [Fact]
    public void Create_EmptyBqId_IsRefused()
    {
        var command = new CreateBoquilhaRegisterCommand(Guid.Empty, Guid.NewGuid());

        Assert.Contains(BoquilhasValidationErrors.BqContextNotFound, BoquilhasValidator.Validate(command));
    }

    [Fact]
    public void Create_RealBqId_IsValid()
    {
        var command = new CreateBoquilhaRegisterCommand(Guid.NewGuid(), Guid.NewGuid());

        Assert.Empty(BoquilhasValidator.Validate(command));
    }

    // ---- history filters --------------------------------------------------------------------

    [Fact]
    public void History_UnknownMachineFilter_IsRefused()
    {
        var query = NewHistory(machine: "X9");

        Assert.Contains(BoquilhasValidationErrors.FilterInvalid, BoquilhasValidator.Validate(query));
    }

    [Theory]
    [InlineData("inicio")]
    [InlineData("irreparavel")]
    [InlineData("ANULAR")]
    public void History_UnknownTypeFilter_IsRefused(string movementType)
    {
        var query = NewHistory(movementType: movementType);

        Assert.Contains(BoquilhasValidationErrors.FilterInvalid, BoquilhasValidator.Validate(query));
    }

    [Fact]
    public void History_InvertedDateRange_IsRefused()
    {
        var query = NewHistory(from: new DateOnly(2026, 9, 30), to: new DateOnly(2026, 9, 1));

        Assert.Contains(BoquilhasValidationErrors.FilterInvalid, BoquilhasValidator.Validate(query));
    }

    [Fact]
    public void History_ThreeClosedTypeFilters_AreValid()
    {
        foreach (var movementType in new[] { "saida", "entrada", "entrada_sem_reparacao" })
        {
            Assert.DoesNotContain(
                BoquilhasValidationErrors.FilterInvalid,
                BoquilhasValidator.Validate(NewHistory(movementType: movementType)));
        }
    }

    [Fact]
    public void History_PageOutOfBounds_IsRefused()
    {
        Assert.Contains(
            BoquilhasValidationErrors.FilterInvalid,
            BoquilhasValidator.Validate(NewHistory(page: 0, pageSize: 50)));

        Assert.Contains(
            BoquilhasValidationErrors.FilterInvalid,
            BoquilhasValidator.Validate(NewHistory(page: 1, pageSize: 101)));

        Assert.DoesNotContain(
            BoquilhasValidationErrors.FilterInvalid,
            BoquilhasValidator.Validate(NewHistory(page: 1, pageSize: 100)));
    }

    // ------------------------------------------------------------------ factories

    private static AppendMovementCommand NewAppend(
        string movementType,
        int quantity,
        DateOnly? businessDate = null,
        string? machine = null,
        Guid? repairerId = null,
        string? observations = null) => new(
        Guid.NewGuid(),
        movementType,
        quantity,
        businessDate ?? new DateOnly(2026, 9, 25),
        machine,
        repairerId,
        observations);

    private static BoquilhasHistoryQuery NewHistory(
        string? machine = null,
        string? movementType = null,
        DateOnly? from = null,
        DateOnly? to = null,
        int page = 1,
        int pageSize = 50) => new(
        Reference: null,
        Lot: null,
        machine,
        from,
        to,
        movementType,
        RepairerId: null,
        page,
        pageSize);
}