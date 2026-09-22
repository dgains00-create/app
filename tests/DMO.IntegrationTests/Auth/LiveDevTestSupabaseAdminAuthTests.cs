using DMO.Application.Authentication;
using DMO.Web.Auth;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DMO.IntegrationTests.Auth;

/// <summary>
/// P1-T02 environment-gated live test — real ADMIN authentication against the DEV/TEST
/// Supabase project.
/// </summary>
/// <remarks>
/// <para>
/// <b>Skipped by default.</b> This is the only P1-T02 test that makes a real network call.
/// It runs only when the operator explicitly opts in with real DEV/TEST configuration, so it
/// can never silently target the DEV/TEST project from a routine test run.
/// </para>
/// <para>
/// It proves authentication only (password grant + user verification through the real
/// <see cref="SupabaseAuthenticationService"/>). It never resolves an application account
/// (that is P1-T03), never creates or mutates anything, and never uses <c>service_role</c>.
/// </para>
/// </remarks>
public sealed class LiveDevTestSupabaseAdminAuthTests
{
    /// <summary>Opt-in flag (<c>1</c> or <c>true</c>) enabling this live test.</summary>
    public const string LiveTestEnvironmentVariable = "DMO_SUPABASE_LIVE_TEST";

    /// <summary>DEV/TEST ADMIN email used only by the live test (never committed).</summary>
    public const string AdminEmailEnvironmentVariable = "DMO_SUPABASE_ADMIN_EMAIL";

    /// <summary>DEV/TEST ADMIN password used only by the live test (never committed).</summary>
    public const string AdminPasswordEnvironmentVariable = "DMO_SUPABASE_ADMIN_PASSWORD";

    [SkippableFact]
    public async Task AdminLogin_LiveDevTestSupabase_AuthenticatesIdentity()
    {
        // Preconditions: explicit opt-in plus real DEV/TEST configuration.
        var enabled = Environment.GetEnvironmentVariable(LiveTestEnvironmentVariable);
        var projectUrl = Environment.GetEnvironmentVariable(SupabaseOptions.ProjectUrlEnvironmentVariable);
        var publishableKey = Environment.GetEnvironmentVariable(SupabaseOptions.PublishableKeyEnvironmentVariable);
        var adminEmail = Environment.GetEnvironmentVariable(AdminEmailEnvironmentVariable);
        var adminPassword = Environment.GetEnvironmentVariable(AdminPasswordEnvironmentVariable);

        Skip.If(
            !string.Equals(enabled, "1", StringComparison.Ordinal)
            && !string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase),
            $"Set {LiveTestEnvironmentVariable}=1 to run the live DEV/TEST Supabase test; skipped by default.");

        Skip.If(
            string.IsNullOrWhiteSpace(projectUrl)
            || string.IsNullOrWhiteSpace(publishableKey)
            || string.IsNullOrWhiteSpace(adminEmail)
            || string.IsNullOrWhiteSpace(adminPassword),
            $"The live test requires {SupabaseOptions.ProjectUrlEnvironmentVariable}, " +
            $"{SupabaseOptions.PublishableKeyEnvironmentVariable}, {AdminEmailEnvironmentVariable} " +
            $"and {AdminPasswordEnvironmentVariable}.");

        // Action: real password-grant authentication (project URL + publishable key only).
        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(projectUrl!.TrimEnd('/') + "/"),
        };
        var service = new SupabaseAuthenticationService(
            httpClient,
            Options.Create(new SupabaseOptions
            {
                ProjectUrl = projectUrl,
                PublishableKey = publishableKey,
            }),
            NullLogger<SupabaseAuthenticationService>.Instance,
            new DMO.IntegrationTests.Auth.Fakes.FakeIntegrationUserAuthenticationLookup());

        var outcome = await service.AuthenticateAsync(
            new AdminLoginRequest(adminEmail!, adminPassword!), CancellationToken.None);

        // Assertions: a real authenticated ADMIN identity is established. No account
        // resolution is performed (persistence is P1-T03). No session is created.
        var authenticated = Assert.IsType<AuthenticationOutcome.Authenticated>(outcome);
        Assert.Equal(AuthenticationPath.Admin, authenticated.Identity.AuthenticationPath);
        Assert.False(string.IsNullOrWhiteSpace(authenticated.Identity.ProviderSubject));
    }
}