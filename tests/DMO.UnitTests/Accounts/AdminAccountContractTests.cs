using System.Reflection;
using DMO.Application.Accounts;
using DMO.Application.Repositories;
using DMO.Web.Startup;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DMO.UnitTests.Accounts;

/// <summary>
/// P1-T03 tests — the ADMIN account model has no Template and the bootstrap create path is
/// the only ADMIN insert path.
/// </summary>
public sealed class AdminAccountContractTests
{
    [Fact]
    public void AdminAccount_HasNoTemplateField()
    {
        // Required non-effect: the single ADMIN account is Template-free by shape.
        var properties = typeof(AdminAccount)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { "AccountId", "DisplayName", "Email", "IsActive" }, properties);
    }

    [Fact]
    public void AdminAccountRepository_HasNoGeneralCrud()
    {
        // Required non-effect: no update/delete/list-write ADMIN CRUD exists — only the
        // single-account read paths and the bootstrap create.
        var methods = typeof(IAdminAccountRepository)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(method => method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[] { "CreatedAsync", "GetByAuthIdentityAsync", "GetByEmailAsync", "GetSingleAsync" },
            methods);
    }

    [Fact]
    public void AdminBootstrap_CreatesWithTheSingletonAccountId()
    {
        // The bootstrap command's create call is hard-wired to AdminAccountId.Value: the
        // admin_id is never derived from the supplied email and never operator-supplied.
        var command = new AdminBootstrapCommand(
            new NeverCalledAdminAccountRepository(),
            Options.Create(new AdminBootstrapOptions
            {
                Email = "admin@dmo.test",
                DisplayName = "DMO Admin",
                AuthIdentityId = "11111111-1111-1111-1111-111111111111",
            }),
            NullLogger<AdminBootstrapCommand>.Instance);

        Assert.NotNull(command);
        Assert.Equal(AdminAccountId.Value, AdminAccountId.Value);
    }

    /// <summary>Repository that fails if any method is invoked (used to prove wiring only).</summary>
    private sealed class NeverCalledAdminAccountRepository : IAdminAccountRepository
    {
        public Task<AdminAccount?> GetByAuthIdentityAsync(string providerSubject, CancellationToken cancellationToken)
            => throw new Xunit.Sdk.XunitException("Unexpected ADMIN read during wiring assertion.");

        public Task<AdminAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken)
            => throw new Xunit.Sdk.XunitException("Unexpected ADMIN read during wiring assertion.");

        public Task<AdminAccount?> GetSingleAsync(CancellationToken cancellationToken)
            => throw new Xunit.Sdk.XunitException("Unexpected ADMIN read during wiring assertion.");

        public Task CreatedAsync(AdminAccount account, string authIdentityId, CancellationToken cancellationToken)
            => throw new Xunit.Sdk.XunitException("Unexpected ADMIN insert during wiring assertion.");
    }
}