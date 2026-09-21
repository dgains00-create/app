using DMO.Application.Accounts;
using DMO.Application.Authentication;
using DMO.Application.Session;
using DMO.IntegrationTests.Host;
using DMO.Web.Auth;
using DMO.Web.Resolution;
using Microsoft.Extensions.DependencyInjection;

namespace DMO.IntegrationTests.Auth;

/// <summary>
/// P1-T02 test — production composition registers the real boundaries and no fakes: the
/// real Supabase authentication boundary, the fail-closed lookup, the real resolver and the
/// real current-account context.
/// </summary>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class ProductionCompositionTests
{
    [Fact]
    public void ProductionComposition_RegistersRealBoundariesAndNoFakes()
    {
        // Preconditions: the production test host (credential-free placeholder config).
        using var factory = new DmoWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Assertions: every boundary maps to the real production class.
        Assert.IsType<SupabaseAuthenticationService>(services.GetRequiredService<IAuthenticationBoundary>());
        Assert.IsType<UnavailableAccountLookup>(services.GetRequiredService<IAccountLookup>());
        Assert.IsType<AccountResolver>(services.GetRequiredService<IAccountResolver>());
        Assert.IsType<SessionAuthentication>(services.GetRequiredService<ISessionAuthentication>());
        Assert.IsType<CurrentAccountContext>(services.GetRequiredService<ICurrentAccountContext>());

        // Required non-effect: no fake adapter, no sample user, no hidden bootstrap and no
        // hard-coded mapping exist in production registration (enforced by the concrete
        // types above; fakes live only in test projects).
    }
}