using DMO.Web.Frontend.Shared.Contracts;

namespace DMO.UnitTests.Frontend.Shared;

/// <summary>
/// P2-T01 (A3) — the shared generic action carrier.
/// Purpose: prove the frozen rule that a disabled action exposes a visible, programmatically
/// associated supplied reason, and that the action key stays opaque (never a canonical identity).
/// Authority: docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md §3 rule 5 and §11.
/// </summary>
public sealed class SharedActionPresentationTests
{
    [Fact]
    public void DisabledAction_WithoutSuppliedReason_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            SharedActionPresentation.Create("open", "Abrir", enabled: false));
        Assert.Throws<ArgumentException>(() =>
            SharedActionPresentation.Create("open", "Abrir", enabled: false, disabledReason: "   "));
    }

    [Fact]
    public void DisabledAction_ExposesItsReason()
    {
        var action = SharedActionPresentation.CreateDisabled("open", "Abrir", "Documento não gerado.");

        Assert.False(action.Enabled);
        Assert.True(action.RequiresDisabledReason);
        Assert.Equal("Documento não gerado.", action.DisabledReason);
        Assert.Equal("disabled", action.StateToken);
    }

    [Fact]
    public void EnabledAction_HasNoReasonAndDistinctToken()
    {
        var action = SharedActionPresentation.CreateEnabled("open", "Abrir");

        Assert.True(action.Enabled);
        Assert.False(action.RequiresDisabledReason);
        Assert.Null(action.DisabledReason);
        Assert.Equal("enabled", action.StateToken);
    }

    [Fact]
    public void BlankKeyOrLabel_Throws()
    {
        Assert.Throws<ArgumentException>(() => SharedActionPresentation.CreateEnabled("  ", "Abrir"));
        Assert.Throws<ArgumentException>(() => SharedActionPresentation.CreateEnabled("open", "  "));
    }

    [Fact]
    public void ActionKey_IsOpaquePresentationData_NotACanonicalIdentity()
    {
        // The key is consumer-controlled and is never one of the canonical identities.
        var action = SharedActionPresentation.CreateEnabled("retry", "Tentar novamente");

        Assert.DoesNotContain("_id", action.Key, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/", action.Key, StringComparison.Ordinal);
    }
}
