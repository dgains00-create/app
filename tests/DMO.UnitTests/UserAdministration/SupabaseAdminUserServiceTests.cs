using System.Net;
using System.Text.Json;
using DMO.Application.UserAdministration;
using DMO.UnitTests.Authentication.Fakes;
using DMO.UnitTests.UserAdministration.Fakes;
using DMO.Web.Auth;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace DMO.UnitTests.UserAdministration;

/// <summary>
/// P1-T05 adapter tests — the real <see cref="SupabaseAdminUserService"/> (privileged
/// ADM-only provider boundary) against a stubbed HTTP transport: admin authentication shape
/// (apikey + Bearer service role), invite body without password/company number, documented
/// pagination with exact match and fail-closed inconsistencies, delete 404 idempotence, the
/// public (service-role-free) recovery path, and secrets never reaching logs.
/// No real network and no real secret is ever involved.
/// </summary>
public sealed class SupabaseAdminUserServiceTests
{
    private const string TestProjectUrl = "https://project-ref.supabase.co";
    private const string TestPublishableKey = "sb_publishable_TEST_KEY";
    private const string TestServiceRoleKey = "sb_secret_SERVICE_ROLE_TEST_KEY";

    [Fact]
    public async Task Invite_SendsAdminAuthenticatedRequest_WithEmailOnly()
    {
        string? capturedBody = null;
        var handler = new FakeHttpMessageHandler(async (request, _) =>
        {
            capturedBody = await request.Content!.ReadAsStringAsync();
            return FakeHttpMessageHandler.JsonResponse(HttpStatusCode.OK, new { id = "subject-1", email = "joao@dmo.test" });
        });
        var service = CreateService(handler);

        var invited = await service.InviteUserAsync("joao@dmo.test", redirectUrl: null, CancellationToken.None);

        Assert.Equal("subject-1", invited.Subject);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/auth/v1/invite", SafePathAndQuery(request));
        Assert.Equal(TestPublishableKey, request.Headers.GetValues("apikey").Single());
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal(TestServiceRoleKey, request.Headers.Authorization.Parameter);

        // The invite body carries the carrier email and nothing else: NO password, NO
        // company number, NO role, NO template.
        using var body = JsonDocument.Parse(capturedBody!);
        Assert.Equal("joao@dmo.test", body.RootElement.GetProperty("email").GetString());
        Assert.False(body.RootElement.TryGetProperty("password", out _));
        Assert.False(body.RootElement.TryGetProperty("company_number", out _));
        Assert.False(body.RootElement.TryGetProperty("companyNumber", out _));
    }

    [Fact]
    public async Task Invite_WithRedirectUrl_AddsRedirectToQuery()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(FakeHttpMessageHandler.JsonResponse(HttpStatusCode.OK, new { id = "subject-1" })));
        var service = CreateService(handler);

        await service.InviteUserAsync("joao@dmo.test", "https://app.dmo.pt/setup", CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Contains("redirect_to=", SafePathAndQuery(request));
        Assert.Contains(Uri.EscapeDataString("https://app.dmo.pt/setup"), SafePathAndQuery(request));
    }

    [Fact]
    public async Task Invite_ConfirmedIdentity_422_MapsToEmailAlreadyInUse()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(FakeHttpMessageHandler.JsonResponse(HttpStatusCode.UnprocessableEntity, new { })));
        var service = CreateService(handler);

        var exception = await Assert.ThrowsAsync<ProviderUserOperationException>(
            () => service.InviteUserAsync("joao@dmo.test", null, CancellationToken.None));

        Assert.Equal(ProviderUserOperationFailure.EmailAlreadyInUse, exception.Failure);
    }

    [Fact]
    public async Task Invite_TransportFailure_MapsToUnavailable()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            throw new HttpRequestException("connection refused"));
        var service = CreateService(handler);

        var exception = await Assert.ThrowsAsync<ProviderUserOperationException>(
            () => service.InviteUserAsync("joao@dmo.test", null, CancellationToken.None));

        Assert.Equal(ProviderUserOperationFailure.Unavailable, exception.Failure);
    }

    [Fact]
    public async Task Invite_ResponseWithoutId_MapsToError()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(FakeHttpMessageHandler.JsonResponse(HttpStatusCode.OK, new { email = "x@dmo.test" })));
        var service = CreateService(handler);

        var exception = await Assert.ThrowsAsync<ProviderUserOperationException>(
            () => service.InviteUserAsync("joao@dmo.test", null, CancellationToken.None));

        Assert.Equal(ProviderUserOperationFailure.Error, exception.Failure);
    }

    [Fact]
    public async Task GetUser_200_ReturnsSubjectAndEmail()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(FakeHttpMessageHandler.JsonResponse(
                HttpStatusCode.OK, new { id = "subject-1", email = "joao@dmo.test" })));
        var service = CreateService(handler);

        var user = await service.GetUserAsync("subject-1", CancellationToken.None);

        Assert.NotNull(user);
        Assert.Equal("subject-1", user!.Subject);
        Assert.Equal("joao@dmo.test", user.Email);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/auth/v1/admin/users/subject-1", SafePathAndQuery(request));
        Assert.Equal(TestServiceRoleKey, request.Headers.Authorization!.Parameter);
    }

    [Fact]
    public async Task GetUser_404_ReturnsNull()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(FakeHttpMessageHandler.JsonResponse(HttpStatusCode.NotFound, new { })));
        var service = CreateService(handler);

        Assert.Null(await service.GetUserAsync("missing", CancellationToken.None));
    }

    [Fact]
    public async Task Find_ExactMatchOnSecondPage_WithoutFilterParameter()
    {
        var page = 0;
        var handler = new FakeHttpMessageHandler((request, _) =>
        {
            page++;
            object[] users;
            if (page == 1)
            {
                // Full documented page (per_page) -> listing is not exhausted yet.
                users = Enumerable.Range(0, SupabaseAdminUserService.ListPageSize)
                    .Select(index => (object)new { id = $"id-{index}", email = $"user{index}@dmo.test" })
                    .ToArray();
            }
            else
            {
                users = new object[]
                {
                    new { id = "id-other", email = "other@dmo.test" },
                    new { id = "subject-1", email = "JOAO@dmo.test" }, // exact normalized match
                };
            }

            return Task.FromResult(FakeHttpMessageHandler.JsonResponse(
                HttpStatusCode.OK, new { aud = "authenticated", users }));
        });
        var service = CreateService(handler);

        var found = await service.FindUserByEmailAsync("  joao@dmo.test ", CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal("subject-1", found!.Subject);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("page=1&per_page=200", SafePathAndQuery(handler.Requests[0]));
        Assert.Contains("page=2&per_page=200", SafePathAndQuery(handler.Requests[1]));

        // The documented paginated listing is scanned; no undocumented email filter is ever
        // sent to the provider.
        foreach (var request in handler.Requests)
        {
            Assert.DoesNotContain("filter", SafePathAndQuery(request));
        }
    }

    [Fact]
    public async Task Find_ZeroMatches_ReturnsNull_WhenListingExhausted()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(FakeHttpMessageHandler.JsonResponse(
                HttpStatusCode.OK,
                new
                {
                    aud = "authenticated",
                    users = new object[]
                    {
                        new { id = "id-1", email = "one@dmo.test" },
                        new { id = "id-2", email = "two@dmo.test" },
                    },
                })));
        var service = CreateService(handler);

        Assert.Null(await service.FindUserByEmailAsync("nobody@dmo.test", CancellationToken.None));
    }

    [Fact]
    public async Task Find_MultipleExactMatches_FailsClosed()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(FakeHttpMessageHandler.JsonResponse(
                HttpStatusCode.OK,
                new
                {
                    aud = "authenticated",
                    users = new object[]
                    {
                        new { id = "subject-1", email = "joao@dmo.test" },
                        new { id = "subject-2", email = "joao@dmo.test" },
                    },
                })));
        var service = CreateService(handler);

        var exception = await Assert.ThrowsAsync<ProviderUserOperationException>(
            () => service.FindUserByEmailAsync("joao@dmo.test", CancellationToken.None));

        // More than one exact match: provider inconsistency, fail closed — never adopt
        // an arbitrary identity.
        Assert.Equal(ProviderUserOperationFailure.Error, exception.Failure);
    }

    [Fact]
    public async Task Find_PageCapExhausted_FailsClosed()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(FakeHttpMessageHandler.JsonResponse(
                HttpStatusCode.OK,
                new
                {
                    aud = "authenticated",
                    users = Enumerable.Range(0, SupabaseAdminUserService.ListPageSize)
                        .Select(index => (object)new { id = $"id-{index}", email = $"user{index}@dmo.test" })
                        .ToArray(),
                })));
        var service = CreateService(handler);

        var exception = await Assert.ThrowsAsync<ProviderUserOperationException>(
            () => service.FindUserByEmailAsync("joao@dmo.test", CancellationToken.None));

        Assert.Equal(ProviderUserOperationFailure.Error, exception.Failure);
        Assert.Equal(SupabaseAdminUserService.MaxLookupPages, handler.Requests.Count);
    }

    [Fact]
    public async Task Delete_204_Succeeds()
    {
        var handler = new FakeHttpMessageHandler((request, _) =>
            Task.FromResult(FakeHttpMessageHandler.JsonResponse(HttpStatusCode.NoContent, new { })));
        var service = CreateService(handler);

        await service.DeleteUserAsync("subject-1", CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal("/auth/v1/admin/users/subject-1", SafePathAndQuery(request));
        Assert.Equal(TestServiceRoleKey, request.Headers.Authorization!.Parameter);
    }

    [Fact]
    public async Task Delete_404_AlreadyAbsent_IsIdempotentSuccess()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(FakeHttpMessageHandler.JsonResponse(HttpStatusCode.NotFound, new { })));
        var service = CreateService(handler);

        await service.DeleteUserAsync("subject-gone", CancellationToken.None); // must not throw
    }

    [Fact]
    public async Task Delete_500_ThrowsError()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(FakeHttpMessageHandler.JsonResponse(HttpStatusCode.InternalServerError, new { })));
        var service = CreateService(handler);

        var exception = await Assert.ThrowsAsync<ProviderUserOperationException>(
            () => service.DeleteUserAsync("subject-1", CancellationToken.None));

        Assert.Equal(ProviderUserOperationFailure.Error, exception.Failure);
    }

    [Fact]
    public async Task Delete_429_ThrowsUnavailable()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(FakeHttpMessageHandler.JsonResponse(HttpStatusCode.TooManyRequests, new { })));
        var service = CreateService(handler);

        var exception = await Assert.ThrowsAsync<ProviderUserOperationException>(
            () => service.DeleteUserAsync("subject-1", CancellationToken.None));

        Assert.Equal(ProviderUserOperationFailure.Unavailable, exception.Failure);
    }

    [Fact]
    public async Task UpdateEmail_SendsAdminPut_WithEmailAndEmailConfirm()
    {
        string? capturedBody = null;
        var handler = new FakeHttpMessageHandler(async (request, _) =>
        {
            capturedBody = await request.Content!.ReadAsStringAsync();
            return FakeHttpMessageHandler.JsonResponse(HttpStatusCode.OK, new { });
        });
        var service = CreateService(handler);

        await service.UpdateUserEmailAsync("subject-1", "novo@dmo.test", CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.Equal("/auth/v1/admin/users/subject-1", SafePathAndQuery(request));
        Assert.Equal(TestServiceRoleKey, request.Headers.Authorization!.Parameter);

        using var body = JsonDocument.Parse(capturedBody!);
        Assert.Equal("novo@dmo.test", body.RootElement.GetProperty("email").GetString());
        Assert.True(body.RootElement.GetProperty("email_confirm").GetBoolean());
    }

    [Fact]
    public async Task Recover_PublicPath_NoServiceRole_EmailBody()
    {
        string? capturedBody = null;
        var handler = new FakeHttpMessageHandler(async (request, _) =>
        {
            capturedBody = await request.Content!.ReadAsStringAsync();
            return FakeHttpMessageHandler.JsonResponse(HttpStatusCode.OK, new { });
        });
        var service = CreateService(handler);

        await service.InitiatePasswordRecoveryAsync("joao@dmo.test", CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/auth/v1/recover", SafePathAndQuery(request));
        Assert.Equal(TestPublishableKey, request.Headers.GetValues("apikey").Single());

        // The PUBLIC recovery path never carries the service-role secret.
        Assert.Null(request.Headers.Authorization);

        using var body = JsonDocument.Parse(capturedBody!);
        Assert.Equal("joao@dmo.test", body.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Secrets_NeverLogged_AcrossFailuresAndSuccesses()
    {
        var handler = new FakeHttpMessageHandler((request, _) =>
            request.RequestUri!.PathAndQuery.Contains("invite")
                ? throw new HttpRequestException("transport down")
                : Task.FromResult(FakeHttpMessageHandler.JsonResponse(
                    HttpStatusCode.TooManyRequests, new { })));
        var logger = new MessageSinkLogger<SupabaseAdminUserService>();
        var service = CreateService(handler, logger);

        // Transport failure (invite) and provider 429 (delete) both log warnings.
        await Assert.ThrowsAsync<ProviderUserOperationException>(
            () => service.InviteUserAsync("joao@dmo.test", null, CancellationToken.None));
        await Assert.ThrowsAsync<ProviderUserOperationException>(
            () => service.DeleteUserAsync("subject-1", CancellationToken.None));

        Assert.NotEmpty(logger.Messages);
        Assert.DoesNotContain(TestServiceRoleKey, logger.Joined, StringComparison.Ordinal);
        Assert.DoesNotContain(TestPublishableKey, logger.Joined, StringComparison.Ordinal);
        Assert.DoesNotContain("joao@dmo.test", logger.Joined, StringComparison.Ordinal);
    }

    [Fact]
    public void AdminOptions_MissingServiceRoleKey_Throws()
    {
        var options = new SupabaseAdminOptions { ServiceRoleKey = null };

        var exception = Assert.Throws<SupabaseConfigurationException>(() => options.Validate());

        Assert.Contains("SupabaseAdmin:ServiceRoleKey", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AdminOptions_Configured_Passes()
    {
        var options = new SupabaseAdminOptions { ServiceRoleKey = TestServiceRoleKey };

        options.Validate(); // must not throw
    }

    private static SupabaseAdminUserService CreateService(
        FakeHttpMessageHandler handler,
        MessageSinkLogger<SupabaseAdminUserService>? logger = null) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri(TestProjectUrl + "/") },
            Options.Create(new SupabaseOptions
            {
                ProjectUrl = TestProjectUrl,
                PublishableKey = TestPublishableKey,
            }),
            Options.Create(new SupabaseAdminOptions { ServiceRoleKey = TestServiceRoleKey }),
            logger ?? new MessageSinkLogger<SupabaseAdminUserService>());

    private static string SafePathAndQuery(HttpRequestMessage request) =>
        request.RequestUri?.PathAndQuery ?? string.Empty;
}