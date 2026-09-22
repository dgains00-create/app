using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using DMO.Application.Authentication;
using DMO.UnitTests.Authentication.Fakes;
using DMO.Web.Auth;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DMO.UnitTests.Authentication;

/// <summary>
/// P1-T02 tests — the real <c>SupabaseAuthenticationService</c> with a fake transport only.
/// No network, no real key, no real credential.
/// </summary>
/// <remarks>
/// The subject under test is the real product logic: request shape (password grant, apikey
/// header), identity verification via the user endpoint, and the honest error mapping. The
/// fake supplies the transport only.
/// </remarks>
public sealed class SupabaseAuthenticationServiceTests
{
    private const string TestProjectUrl = "https://test.invalid";
    private const string TestPublishableKey = "sb_publishable_test_key";

    private static SupabaseAuthenticationService CreateService(
        FakeHttpMessageHandler handler,
        IUserAuthenticationLookup? lookup = null)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(TestProjectUrl + "/") };

        return new SupabaseAuthenticationService(
            httpClient,
            Options.Create(new SupabaseOptions
            {
                ProjectUrl = TestProjectUrl,
                PublishableKey = TestPublishableKey,
            }),
            NullLogger<SupabaseAuthenticationService>.Instance,
            lookup ?? new FakeUserAuthenticationLookup(new UserLoginIdentity("carrier@dmo.test")));
    }

    /// <summary>
    /// Happy-path transport that captures the password-grant body at exchange time (the
    /// service disposes its request after the call; asserting on a disposed body throws).
    /// </summary>
    private sealed class HappyPathExchange
    {
        public JsonElement? PostBody { get; private set; }

        public FakeHttpMessageHandler Handler { get; }

        public HappyPathExchange()
        {
            Handler = new FakeHttpMessageHandler(async (request, cancellationToken) =>
            {
                if (request.Method == HttpMethod.Post)
                {
                    PostBody = await request.Content!.ReadFromJsonAsync<JsonElement>(cancellationToken);
                    return FakeHttpMessageHandler.JsonResponse(
                        HttpStatusCode.OK, new { access_token = "tok-do-not-leak" });
                }

                return FakeHttpMessageHandler.JsonResponse(HttpStatusCode.OK, new { id = "verified-user-id" });
            });
        }
    }

    private static FakeHttpMessageHandler HappyPathHandler() =>
        new((request, _) =>
        {
            if (request.Method == HttpMethod.Post)
            {
                return Task.FromResult(FakeHttpMessageHandler.JsonResponse(
                    HttpStatusCode.OK, new { access_token = "tok-do-not-leak" }));
            }

            return Task.FromResult(FakeHttpMessageHandler.JsonResponse(
                HttpStatusCode.OK, new { id = "verified-user-id" }));
        });

    [Fact]
    public async Task AdminRequest_IssuesGoTruePasswordGrant()
    {
        // Preconditions: a valid ADMIN login request.
        var exchange = new HappyPathExchange();
        var service = CreateService(exchange.Handler);

        // Action: authenticate.
        var outcome = await service.AuthenticateAsync(
            new AdminLoginRequest("admin@dmo.test", "correct-password"), CancellationToken.None);

        // Assertions: the identity is established after the token + verification exchange.
        var authenticated = Assert.IsType<AuthenticationOutcome.Authenticated>(outcome);
        Assert.Equal("verified-user-id", authenticated.Identity.ProviderSubject);
        Assert.Equal(AuthenticationPath.Admin, authenticated.Identity.AuthenticationPath);

        // The first request is exactly the GoTrue password grant.
        var tokenRequest = Assert.Single(exchange.Handler.Requests.Take(1));
        Assert.Equal(HttpMethod.Post, tokenRequest.Method);
        Assert.Equal(new Uri($"{TestProjectUrl}/auth/v1/token?grant_type=password"), tokenRequest.RequestUri);

        // The publishable key travels only in the apikey header.
        Assert.Equal(TestPublishableKey, tokenRequest.Headers.GetValues("apikey").Single());

        // The body carries the email + password pair (never a company number, never a token).
        var body = Assert.IsType<JsonElement>(exchange.PostBody);
        Assert.Equal("admin@dmo.test", body.GetProperty("email").GetString());
        Assert.Equal("correct-password", body.GetProperty("password").GetString());
    }

    [Fact]
    public async Task VerifiesIdentityViaUserEndpoint()
    {
        // Preconditions: token success.
        var handler = HappyPathHandler();
        var service = CreateService(handler);

        // Action: authenticate.
        var outcome = await service.AuthenticateAsync(
            new AdminLoginRequest("admin@dmo.test", "correct-password"), CancellationToken.None);

        // Assertions: the second call is the authenticated user verification, and the
        // identity subject is derived from that verified response.
        Assert.IsType<AuthenticationOutcome.Authenticated>(outcome);
        Assert.Equal(2, handler.Requests.Count);

        var verifyRequest = handler.Requests[1];
        Assert.Equal(HttpMethod.Get, verifyRequest.Method);
        Assert.Equal(new Uri($"{TestProjectUrl}/auth/v1/user"), verifyRequest.RequestUri);

        // Verification carries the access token as a Bearer token (never as the key).
        Assert.Equal("Bearer tok-do-not-leak", verifyRequest.Headers.Authorization!.ToString());
        Assert.Equal(TestPublishableKey, verifyRequest.Headers.GetValues("apikey").Single());
    }

    [Fact]
    public async Task InvalidCredentials_MapsToInvalidCredentials()
    {
        // Preconditions: the provider rejects the credentials (HTTP 400).
        var handler = FakeHttpMessageHandler.Returning(FakeHttpMessageHandler.JsonResponse(
            HttpStatusCode.BadRequest, new { code = "invalid_credentials", message = "Invalid login credentials" }));
        var service = CreateService(handler);

        // Action: authenticate.
        var outcome = await service.AuthenticateAsync(
            new AdminLoginRequest("admin@dmo.test", "wrong-password"), CancellationToken.None);

        // Assertions: exactly the credential fact; no verification call is made.
        var failed = Assert.IsType<AuthenticationOutcome.Failed>(outcome);
        Assert.Equal(AuthenticationFailureReason.InvalidCredentials, failed.Reason);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task UnexpectedNonCredential4xx_MapsToProviderError(HttpStatusCode statusCode)
    {
        // Preconditions: the provider returns an unexpected non-credential 4xx (403/404) on
        // the password grant — a provider/protocol/configuration fact, never a statement about
        // the human's credentials.
        var handler = FakeHttpMessageHandler.Returning(FakeHttpMessageHandler.JsonResponse(
            statusCode, new { message = "nope" }));
        var service = CreateService(handler);

        // Action: authenticate.
        var outcome = await service.AuthenticateAsync(
            new AdminLoginRequest("admin@dmo.test", "correct-password"), CancellationToken.None);

        // Assertions: reported as ProviderError, never InvalidCredentials; only the token
        // request is issued, with no verification call after the failure.
        var failed = Assert.IsType<AuthenticationOutcome.Failed>(outcome);
        Assert.Equal(AuthenticationFailureReason.ProviderError, failed.Reason);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task ServerError_MapsToProviderError()
    {
        // Preconditions: the provider returns a 5xx.
        var handler = FakeHttpMessageHandler.Returning(FakeHttpMessageHandler.JsonResponse(
            HttpStatusCode.InternalServerError, new { message = "boom" }));
        var service = CreateService(handler);

        var outcome = await service.AuthenticateAsync(
            new AdminLoginRequest("admin@dmo.test", "correct-password"), CancellationToken.None);

        var failed = Assert.IsType<AuthenticationOutcome.Failed>(outcome);
        Assert.Equal(AuthenticationFailureReason.ProviderError, failed.Reason);
    }

    [Fact]
    public async Task TransportFailure_MapsToProviderUnavailable()
    {
        // Preconditions: the transport fails before any HTTP response.
        var handler = new FakeHttpMessageHandler((_, _) => throw new HttpRequestException("network unreachable"));
        var service = CreateService(handler);

        var outcome = await service.AuthenticateAsync(
            new AdminLoginRequest("admin@dmo.test", "correct-password"), CancellationToken.None);

        var failed = Assert.IsType<AuthenticationOutcome.Failed>(outcome);
        Assert.Equal(AuthenticationFailureReason.ProviderUnavailable, failed.Reason);
    }

    [Fact]
    public async Task UserRequest_UnknownCompanyNumber_RejectedWithoutProviderCall()
    {
        // Preconditions: a USER login request whose company number has no persisted mapping
        // (the lookup returns no carrier). The USER flow is real since P1-T03.
        var handler = HappyPathHandler();
        var lookup = new FakeUserAuthenticationLookup(_ => null);
        var service = CreateService(handler, lookup);

        // Action: authenticate.
        var outcome = await service.AuthenticateAsync(
            new UserLoginRequest("2661", "secret"), CancellationToken.None);

        // Assertions: invalid credentials with no provider round trip; company_number is
        // never transformed into an email and no synthetic email is built.
        var failed = Assert.IsType<AuthenticationOutcome.Failed>(outcome);
        Assert.Equal(AuthenticationFailureReason.InvalidCredentials, failed.Reason);
        Assert.Empty(handler.Requests);
        Assert.Equal("2661", Assert.Single(lookup.CompanyNumbers));
    }

    [Fact]
    public async Task NoTokenLeaksIntoApplication()
    {
        // Preconditions: a successful exchange returns an access token the application must
        // not consume.
        var handler = HappyPathHandler();
        var service = CreateService(handler);

        var outcome = await service.AuthenticateAsync(
            new AdminLoginRequest("admin@dmo.test", "correct-password"), CancellationToken.None);

        // Assertions: the outcome carries only ProviderSubject + AuthenticationPath.
        var authenticated = Assert.IsType<AuthenticationOutcome.Authenticated>(outcome);
        Assert.Equal("verified-user-id", authenticated.Identity.ProviderSubject);
        Assert.Equal(AuthenticationPath.Admin, authenticated.Identity.AuthenticationPath);

        // The token value never appears in the application-facing identity.
        Assert.DoesNotContain(
            "tok-do-not-leak",
            authenticated.Identity.ProviderSubject,
            StringComparison.Ordinal);

        // The identity type exposes exactly the two accepted members.
        var identityMembers = typeof(AuthenticatedIdentity)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "AuthenticationPath", "ProviderSubject" }, identityMembers);
    }

    [Fact]
    public void MissingSupabaseConfiguration_FailsLoud()
    {
        // Preconditions: no Supabase configuration at all.
        var options = new SupabaseOptions();

        // Action + assertion: validation refuses rather than defaulting.
        var exception = Assert.Throws<SupabaseConfigurationException>(() => options.Validate());
        Assert.Contains(SupabaseOptions.ProjectUrlKey, exception.Message, StringComparison.Ordinal);

        // Public key missing while the URL is present.
        var optionsWithUrlOnly = new SupabaseOptions { ProjectUrl = TestProjectUrl };
        var keyException = Assert.Throws<SupabaseConfigurationException>(() => optionsWithUrlOnly.Validate());
        Assert.Contains(SupabaseOptions.PublishableKeyKey, keyException.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BlankAdminCredentials_RejectedWithoutHttpCall()
    {
        // Preconditions: blank email or password.
        var handler = HappyPathHandler();
        var service = CreateService(handler);

        // Action: authenticate with blank values.
        var outcome = await service.AuthenticateAsync(
            new AdminLoginRequest("   ", "x"), CancellationToken.None);

        // Assertions: rejected as invalid credentials without contacting the provider.
        var failed = Assert.IsType<AuthenticationOutcome.Failed>(outcome);
        Assert.Equal(AuthenticationFailureReason.InvalidCredentials, failed.Reason);
        Assert.Empty(handler.Requests);
    }
}