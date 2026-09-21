using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DMO.Application.Accounts;
using DMO.Application.Authentication;
using DMO.Infrastructure;
using DMO.Infrastructure.Database;
using DMO.IntegrationTests.Host;
using DMO.Web.Auth;
using DMO.Web.Endpoints;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DMO.IntegrationTests.Auth;

/// <summary>
/// P1-T02 production-posture tests — what actually happens when production composition is
/// used: ADMIN authentication transport is stubbed at the HTTP boundary (never the real
/// project), resolution stays the real fail-closed lookup, and the USER flow is declared
/// unavailable.
/// </summary>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class ProductionFailClosedTests
{
    [Fact]
    public async Task ProductionLogin_Admin_FailsClosedAtAccountStep()
    {
        // Preconditions: production composition, with ONLY the Supabase transport stubbed
        // (the real SupabaseAuthenticationService logic runs over a fake transport; account
        // resolution is the real AccountResolver + UnavailableAccountLookup).
        using var factory = new DmoWebApplicationFactory().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<SupabaseAuthenticationService>();
                services.RemoveAll<IAuthenticationBoundary>();
                services.AddScoped<SupabaseAuthenticationService>(provider =>
                    new SupabaseAuthenticationService(
                        new HttpClient(new StubTransportHandler())
                        {
                            BaseAddress = new Uri("https://dmo-test-placeholder.invalid/"),
                        },
                        provider.GetRequiredService<IOptions<SupabaseOptions>>(),
                        provider.GetRequiredService<ILogger<SupabaseAuthenticationService>>()));
                services.AddScoped<IAuthenticationBoundary>(provider =>
                    provider.GetRequiredService<SupabaseAuthenticationService>());
            }));
        using var client = factory.CreateClient();

        // Action: a credentials-correct ADMIN authentication attempt.
        var login = await client.PostAsJsonAsync(
            AuthEndpoints.LoginPath, new { email = "admin@dmo.test", password = "secret" });

        // Assertions: authentication can succeed, but account resolution fails closed until
        // P1-T03 supplies the durable ADMIN mapping — no session is established.
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);

        var me = await client.GetFromJsonAsync<JsonElement>(AuthEndpoints.CurrentAccountPath);
        Assert.Equal("none", me.GetProperty("accountType").GetString());
    }

    [Fact]
    public async Task ProductionLogin_User_FailsClosedNoUserProviderFlow()
    {
        // Preconditions: pure production composition — no test replacement at all.
        using var factory = new DmoWebApplicationFactory();
        using var client = factory.CreateClient();

        // Action: a company_number + password USER attempt.
        var login = await client.PostAsJsonAsync(
            AuthEndpoints.LoginPath, new { companyNumber = "2661", password = "secret" });

        // Assertions: the USER provider flow is declared unavailable in P1-T02 (the durable
        // mapping is P1-T03); the company number is never turned into an email.
        Assert.Equal(HttpStatusCode.ServiceUnavailable, login.StatusCode);

        var me = await client.GetFromJsonAsync<JsonElement>(AuthEndpoints.CurrentAccountPath);
        Assert.Equal("none", me.GetProperty("accountType").GetString());
    }

    [Fact]
    public void ProductionStartup_WithoutSupabaseConfig_FailsLoud()
    {
        // Preconditions: database configuration valid, Supabase configuration absent. This
        // mirrors the StartupConfigurationTests precedent — the composition boundary is
        // tested directly because the entry point's original exception type is not the
        // acceptance contract.
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [DatabaseOptions.ConnectionStringKey] = DmoWebApplicationFactory.PlaceholderConnectionString,
            })
            .Build();

        // Database configuration is valid, so the only missing piece is Supabase.
        services.AddDmoInfrastructure(configuration);

        var supabaseOptions = new SupabaseOptions();
        configuration.GetSection(SupabaseOptions.SectionName).Bind(supabaseOptions);

        // Action + assertion: absent Supabase configuration fails loudly with the key named.
        var exception = Assert.Throws<SupabaseConfigurationException>(() => supabaseOptions.Validate());
        Assert.Contains(SupabaseOptions.ProjectUrlKey, exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Test-only transport stub answering the GoTrue token + user endpoints with fixed
    /// success bodies. Never touches the real project.
    /// </summary>
    private sealed class StubTransportHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.Method == HttpMethod.Post)
            {
                return Task.FromResult(Json(HttpStatusCode.OK, new { access_token = "stubbed-token" }));
            }

            return Task.FromResult(Json(HttpStatusCode.OK, new { id = "stubbed-admin-subject" }));
        }

        private static HttpResponseMessage Json(HttpStatusCode statusCode, object body) =>
            new(statusCode) { Content = JsonContent.Create(body) };
    }
}