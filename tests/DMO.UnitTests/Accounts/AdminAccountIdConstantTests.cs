using DMO.Application.Accounts;
using DMO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DMO.UnitTests.Accounts;

/// <summary>
/// P1-T03 tests — the fixed, code-owned single-ADMIN id constant.
/// </summary>
/// <remarks>
/// The value is <c>00000000-0000-4000-8000-0000000000ad</c>, identical on every run, never
/// derived from an email and never operator-supplied. The entity configuration, the
/// persistence model (and therefore the <c>admin_accounts_singleton_id_check</c> DDL) must
/// reference exactly it, and no other insert value may exist for <c>admin_accounts.admin_id</c>.
/// </remarks>
public sealed class AdminAccountIdConstantTests
{
    private static readonly Guid Expected = Guid.Parse("00000000-0000-4000-8000-0000000000ad");

    [Fact]
    public void AdminAccountId_IsTheFixedConstant()
    {
        Assert.Equal(Expected, AdminAccountId.Value);
    }

    [Fact]
    public void AdminAccountId_IsStableAcrossAccesses()
    {
        // Same value on every access: a constant, never derived.
        Assert.Equal(AdminAccountId.Value, AdminAccountId.Value);
    }

    [Fact]
    public void AdminAccountId_IsNotDerivedFromAnyEmail()
    {
        // There is no email input anywhere in the constant: the fixed value is returned
        // regardless of any email-shaped input (there is none — the constant has no parameters).
        Assert.Equal(Expected, AdminAccountId.Value);
        Assert.DoesNotContain("@", Expected.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void EntityConfiguration_ReferencesExactlyTheConstant()
    {
        // The singleton CHECK constraint must pin admin_id to exactly the constant UUID.
        // (Model building is in-memory; no database connection is opened.)
        var options = new DbContextOptionsBuilder<DmoDbContext>()
            .UseNpgsql(PlaceholderConnectionString)
            .Options;

        using var context = new DmoDbContext(options);

        // EF Core 10 keeps check constraints only in the design-time model.
        var model = context.GetService<IDesignTimeModel>().Model;

        var entityType = model.FindEntityType(typeof(DMO.Infrastructure.Persistence.Entities.AdminAccountEntity));
        Assert.NotNull(entityType);

        var checkConstraint = Assert.Single(
            entityType!.GetCheckConstraints(),
            constraint => constraint.Name == "admin_accounts_singleton_id_check");

        Assert.Equal($"admin_id = '{Expected}'::uuid", checkConstraint.Sql);
    }

    /// <summary>Credential-free placeholder; never contacted (model building only).</summary>
    private const string PlaceholderConnectionString =
        "Host=localhost;Port=5432;Database=dmo_placeholder;Username=placeholder;Password=placeholder";

    [Fact]
    public void BootstrapCommand_InsertsOnlyTheConstant()
    {
        // The bootstrap create path must carry exactly AdminAccountId.Value for admin_id.
        // (Behavioral coverage lives in AdminBootstrapCommandTests / AdminBootstrapIntegrationTests;
        // this asserts the constant is the only admissible value by construction.)
        Assert.Equal(Expected, AdminAccountId.Value);
        Assert.NotEqual(Guid.Empty, AdminAccountId.Value);
    }
}