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
/// P1-T03 tests — the real <c>SupabaseAuthenticationService</c> USER flow with a fake
/// transport and a fake <see cref="IUserAuthenticationLookup"/> only. No network, no real
/// key, no real credential.
/// </summary>
/// <remarks>
/// The subject under test is the real product logic: company_number → persisted carrier email
/// → Supabase password grant (the grant body never carries the company_number), identity
/// verification via the user endpoint, honest error mapping, and the "unknown company number
/// → InvalidCredentials without a provider call" rule.
/// </remarks>
public sealed class SupabaseAuthenticationServiceUserFlowTests
{
    private const string TestProjectUrl = "https://test.invalid";
    private const string TestPublishableKey = "sb_publishable_test_key";
    private const string TestCompanyNumber = "2661";
    private const string TestCarrierEmail = "carrier@dmo.test";

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
            lookup ?? new FakeUserAuthenticationLookup(new UserLoginIdentity(TestCarrierEmail)));
    }

    /// <summary>
    /// Happy-path transport that captures the password-grant body at exchange time (the
    /// service disposes its request after the call; asserting on a disposed body throws).
    /// </summary>
    private sealed class HappyPathExchange
    {
        public JsonElement? PostBody { get; private set; }

        public FakeHttpMessageHandler Handler { get; }

        public HappyPathExchange(string verifiedSubject = "verified-user-id")
        {
            Handler = new FakeHttpMessageHandler(async (request, cancellationToken) =>
            {
                if (request.Method == HttpMethod.Post)
                {
                    PostBody = await request.Content!.ReadFromJsonAsync<JsonElement>(cancellationToken);
                    return FakeHttpMessageHandler.JsonResponse(
                        HttpStatusCode.OK, new { access_token = "tok-do-not-leak" });
                }

                return FakeHttpMessageHandler.JsonResponse(HttpStatusCode.OK, new { id = verifiedSubject });
            });
        }
    }

    [Fact]
    public async Task UserRequest_GrantBodyUsesCarrierEmail_NeverTheCompanyNumber()
    {
        // Preconditions: the company number maps to a persisted carrier email.
        var exchange = new HappyPathExchange();
        var lookup = new FakeUserAuthenticationLookup(new UserLoginIdentity(TestCarrierEmail));
        var service = CreateService(exchange.Handler, lookup);

        // Action: USER login with company_number + password.
        var outcome = await service.AuthenticateAsync(
            new UserLoginRequest(TestCompanyNumber, "correct-password"), CancellationToken.None);

        // Assertions: identity established on the User path from the verified subject.
        var authenticated = Assert.IsType<AuthenticationOutcome.Authenticated>(outcome);
        Assert.Equal("verified-user-id", authenticated.Identity.ProviderSubject);
        Assert.Equal(AuthenticationPath.User, authenticated.Identity.AuthenticationPath);

        // The grant body carries the persisted carrier email — never the company number,
        // never a synthetic email.
        var body = Assert.IsType<JsonElement>(exchange.PostBody);
        Assert.Equal(TestCarrierEmail, body.GetProperty("email").GetString());
        Assert.Equal("correct-password", body.GetProperty("password").GetString());
        Assert.DoesNotContain(TestCompanyNumber, body.GetProperty("email").GetString()!);
        Assert.DoesNotContain("@", TestCompanyNumber, StringComparison.Ordinal);

        // The exact company number was passed to the narrow lookup, unchanged.
        Assert.Equal(TestCompanyNumber, Assert.Single(lookup.CompanyNumbers));
    }

    [Fact]
    public async Task VerifiesIdentityViaUserEndpoint_UserPath()
    {
        var handler = new HappyPathExchange().Handler;
        var service = CreateService(handler);

        var outcome = await service.AuthenticateAsync(
            new UserLoginRequest(TestCompanyNumber, "correct-password"), CancellationToken.None);

        Assert.IsType<AuthenticationOutcome.Authenticated>(outcome);
        Assert.Equal(2, handler.Requests.Count);

        var verifyRequest = handler.Requests[1];
        Assert.Equal(HttpMethod.Get, verifyRequest.Method);
        Assert.Equal(new Uri($"{TestProjectUrl}/auth/v1/user"), verifyRequest.RequestUri);
        Assert.Equal("Bearer tok-do-not-leak", verifyRequest.Headers.Authorization!.ToString());
        Assert.Equal(TestPublishableKey, verifyRequest.Headers.GetValues("apikey").Single());
    }

    [Fact]
    public async Task UnknownCompanyNumber_FailsInvalidCredentials_WithNoProviderCall()
    {
        // Preconditions: the lookup has no mapping for the company number.
        var handler = new HappyPathExchange().Handler;
        var lookup = new FakeUserAuthenticationLookup(_ => null);
        var service = CreateService(handler, lookup);

        // Action: USER login with an unknown company number.
        var outcome = await service.AuthenticateAsync(
            new UserLoginRequest("999999", "secret"), CancellationToken.None);

        // Assertions: invalid credentials with ZERO provider requests (no round trip).
        var failed = Assert.IsType<AuthenticationOutcome.Failed>(outcome);
        Assert.Equal(AuthenticationFailureReason.InvalidCredentials, failed.Reason);
        Assert.Empty(handler.Requests);
        Assert.Equal("999999", Assert.Single(lookup.CompanyNumbers));
    }

    [Fact]
    public async Task LookupFailure_MapsToProviderUnavailable_NoProviderCall()
    {
        var handler = new HappyPathExchange().Handler;
        var lookup = FakeUserAuthenticationLookup.Throwing(new InvalidOperationException("db down"));
        var service = CreateService(handler, lookup);

        var outcome = await service.AuthenticateAsync(
            new UserLoginRequest(TestCompanyNumber, "secret"), CancellationToken.None);

        var failed = Assert.IsType<AuthenticationOutcome.Failed>(outcome);
        Assert.Equal(AuthenticationFailureReason.ProviderUnavailable, failed.Reason);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task GrantCredentialRejection_MapsToInvalidCredentials(HttpStatusCode statusCode)
    {
        var handler = FakeHttpMessageHandler.Returning(FakeHttpMessageHandler.JsonResponse(
            statusCode, new { code = "invalid_credentials" }));
        var service = CreateService(handler);

        var outcome = await service.AuthenticateAsync(
            new UserLoginRequest(TestCompanyNumber, "wrong-password"), CancellationToken.None);

        var failed = Assert.IsType<AuthenticationOutcome.Failed>(outcome);
        Assert.Equal(AuthenticationFailureReason.InvalidCredentials, failed.Reason);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task RateLimited_MapsToProviderUnavailable()
    {
        var handler = FakeHttpMessageHandler.Returning(FakeHttpMessageHandler.JsonResponse(
            HttpStatusCode.TooManyRequests, new { message = "slow down" }));
        var service = CreateService(handler);

        var outcome = await service.AuthenticateAsync(
            new UserLoginRequest(TestCompanyNumber, "secret"), CancellationToken.None);

        var failed = Assert.IsType<AuthenticationOutcome.Failed>(outcome);
        Assert.Equal(AuthenticationFailureReason.ProviderUnavailable, failed.Reason);
    }

    [Fact]
    public async Task ServerError_MapsToProviderError()
    {
        var handler = FakeHttpMessageHandler.Returning(FakeHttpMessageHandler.JsonResponse(
            HttpStatusCode.InternalServerError, new { message = "boom" }));
        var service = CreateService(handler);

        var outcome = await service.AuthenticateAsync(
            new UserLoginRequest(TestCompanyNumber, "secret"), CancellationToken.None);

        var failed = Assert.IsType<AuthenticationOutcome.Failed>(outcome);
        Assert.Equal(AuthenticationFailureReason.ProviderError, failed.Reason);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task UnexpectedNonCredential4xx_MapsToProviderError(HttpStatusCode statusCode)
    {
        var handler = FakeHttpMessageHandler.Returning(FakeHttpMessageHandler.JsonResponse(
            statusCode, new { message = "nope" }));
        var service = CreateService(handler);

        var outcome = await service.AuthenticateAsync(
            new UserLoginRequest(TestCompanyNumber, "secret"), CancellationToken.None);

        var failed = Assert.IsType<AuthenticationOutcome.Failed>(outcome);
        Assert.Equal(AuthenticationFailureReason.ProviderError, failed.Reason);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task TransportFailure_MapsToProviderUnavailable()
    {
        var handler = new FakeHttpMessageHandler((_, _) => throw new HttpRequestException("network unreachable"));
        var service = CreateService(handler);

        var outcome = await service.AuthenticateAsync(
            new UserLoginRequest(TestCompanyNumber, "secret"), CancellationToken.None);

        var failed = Assert.IsType<AuthenticationOutcome.Failed>(outcome);
        Assert.Equal(AuthenticationFailureReason.ProviderUnavailable, failed.Reason);
    }

    [Fact]
    public async Task VerificationSubjectAbsent_MapsToProviderError()
    {
        var handler = new FakeHttpMessageHandler((request, _) =>
        {
            if (request.Method == HttpMethod.Post)
            {
                return Task.FromResult(FakeHttpMessageHandler.JsonResponse(
                    HttpStatusCode.OK, new { access_token = "tok-do-not-leak" }));
            }

            return Task.FromResult(FakeHttpMessageHandler.JsonResponse(HttpStatusCode.OK, new { }));
        });
        var service = CreateService(handler);

        var outcome = await service.AuthenticateAsync(
            new UserLoginRequest(TestCompanyNumber, "secret"), CancellationToken.None);

        var failed = Assert.IsType<AuthenticationOutcome.Failed>(outcome);
        Assert.Equal(AuthenticationFailureReason.ProviderError, failed.Reason);
    }

    [Fact]
    public async Task NoTokenLeaksIntoApplication_UserPath()
    {
        var handler = new HappyPathExchange().Handler;
        var service = CreateService(handler);

        var outcome = await service.AuthenticateAsync(
            new UserLoginRequest(TestCompanyNumber, "secret"), CancellationToken.None);

        var authenticated = Assert.IsType<AuthenticationOutcome.Authenticated>(outcome);

        // The identity exposes exactly the two accepted members and never the token value.
        var identityMembers = typeof(AuthenticatedIdentity)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "AuthenticationPath", "ProviderSubject" }, identityMembers);
        Assert.DoesNotContain(
            "tok-do-not-leak",
            authenticated.Identity.ProviderSubject,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task BlankCompanyNumberOrPassword_Rejected_NoLookupNoHttp()
    {
        // Preconditions: blank company number or blank password.
        var handler = new HappyPathExchange().Handler;
        var lookup = new FakeUserAuthenticationLookup(new UserLoginIdentity(TestCarrierEmail));
        var service = CreateService(handler, lookup);

        // Action: authenticate with blank values.
        var blankCompany = await service.AuthenticateAsync(
            new UserLoginRequest("   ", "secret"), CancellationToken.None);
        var blankPassword = await service.AuthenticateAsync(
            new UserLoginRequest(TestCompanyNumber, "  "), CancellationToken.None);

        // Assertions: rejected as invalid credentials without touching persistence or the provider.
        Assert.Equal(AuthenticationFailureReason.InvalidCredentials,
            Assert.IsType<AuthenticationOutcome.Failed>(blankCompany).Reason);
        Assert.Equal(AuthenticationFailureReason.InvalidCredentials,
            Assert.IsType<AuthenticationOutcome.Failed>(blankPassword).Reason);
        Assert.Empty(lookup.CompanyNumbers);
        Assert.Empty(handler.Requests);
    }
}