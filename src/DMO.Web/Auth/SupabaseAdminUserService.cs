using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DMO.Application.UserAdministration;
using Microsoft.Extensions.Options;

namespace DMO.Web.Auth;

/// <summary>
/// Production <see cref="IUserIdentityProvisioner"/> — the single server-only privileged
/// boundary to the Supabase Auth <b>Admin API</b>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Transport</b> (verified against the current official Supabase documentation and the
/// current official client sources):
/// </para>
/// <code>
/// invite      : POST   {ProjectUrl}/auth/v1/invite                     (admin: Bearer service role)
///                body { email, data } — NO password, NO company number
///                200 -> user { id } (subject); unconfirmed identity -> re-send; confirmed -> 422
/// get         : GET    {ProjectUrl}/auth/v1/admin/users/{subject}
/// find        : GET    {ProjectUrl}/auth/v1/admin/users?page=&amp;per_page=   (documented pagination;
///                exact normalized-email match server-side; undocumented filter never used)
/// update email: PUT    {ProjectUrl}/auth/v1/admin/users/{subject} { email, email_confirm: true }
/// delete      : DELETE {ProjectUrl}/auth/v1/admin/users/{subject}   (404 = already absent = success)
/// recover     : POST   {ProjectUrl}/auth/v1/recover  { email }      (PUBLIC: apikey only, NO service role)
/// </code>
/// <para>
/// <b>Secrets</b>: every Admin call carries <c>apikey</c> (publishable) and
/// <c>Authorization: Bearer {service role}</c>. The service-role secret comes from
/// <see cref="SupabaseAdminOptions"/> (server-only), is never logged and never leaves these
/// request headers. The public <c>recover</c> path carries only the publishable key.
/// </para>
/// <para>
/// Failures surface as <see cref="ProviderUserOperationException"/> with a typed
/// <see cref="ProviderUserOperationFailure"/>; no HTTP/JSON ever reaches Application code.
/// </para>
/// </remarks>
public sealed class SupabaseAdminUserService : IUserIdentityProvisioner
{
    /// <summary>GoTrue invitation endpoint (admin-only), relative to the project URL.</summary>
    public const string InviteEndpointPath = "auth/v1/invite";

    /// <summary>GoTrue admin users collection/identity endpoint, relative to the project URL.</summary>
    public const string AdminUsersEndpointPath = "auth/v1/admin/users";

    /// <summary>GoTrue public recovery endpoint (no service role), relative to the project URL.</summary>
    public const string RecoverEndpointPath = "auth/v1/recover";

    /// <summary>Explicit page size for the documented provider list (≤ the accepted maximum).</summary>
    public const int ListPageSize = 200;

    /// <summary>
    /// Hard cap on provider list pages scanned for one lookup: beyond it the listing is not
    /// provably exhausted, so the lookup fails closed instead of guessing.
    /// </summary>
    public const int MaxLookupPages = 10;

    private readonly HttpClient _httpClient;
    private readonly SupabaseOptions _options;
    private readonly SupabaseAdminOptions _adminOptions;
    private readonly ILogger<SupabaseAdminUserService> _logger;

    /// <summary>Creates the service over the configured HTTP client and both Supabase option sets.</summary>
    public SupabaseAdminUserService(
        HttpClient httpClient,
        IOptions<SupabaseOptions> options,
        IOptions<SupabaseAdminOptions> adminOptions,
        ILogger<SupabaseAdminUserService> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(adminOptions);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _options = options.Value;
        _adminOptions = adminOptions.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ProviderUserInvited> InviteUserAsync(
        string carrierEmail,
        string? redirectUrl,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(carrierEmail);

        var path = InviteEndpointPath;
        if (!string.IsNullOrWhiteSpace(redirectUrl))
        {
            // The invitation-link redirection travels as the documented redirect_to query.
            path += $"?redirect_to={Uri.EscapeDataString(redirectUrl)}";
        }

        using var request = CreateAdminRequest(HttpMethod.Post, path);
        request.Content = JsonContent.Create(new
        {
            email = carrierEmail,
            // data is present as an empty object; there is NOTHING else in the body —
            // no password, no company number, no role, no template, no token.
            data = new { },
        });

        using var response = await SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            // 422: the provider already holds a CONFIRMED identity for the carrier email (an
            // unconfirmed identity gets a 200 re-send instead). The adapter maps the HTTP
            // fact; the service interprets create-vs-resend context.
            throw new ProviderUserOperationException(
                ProviderUserOperationFailure.EmailAlreadyInUse,
                "Provider rejected the invitation: a confirmed identity already exists for the carrier email.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw MapFailure(response, "provider invite");
        }

        var invited = await ReadUserAsync(response, cancellationToken);
        if (string.IsNullOrWhiteSpace(invited?.Id))
        {
            throw new ProviderUserOperationException(
                ProviderUserOperationFailure.Error,
                "Provider invite response carried no identity id.");
        }

        _logger.LogInformation("Provider invitation completed (status 200).");
        return new ProviderUserInvited(invited.Id);
    }

    /// <inheritdoc />
    public async Task<ProviderUser?> GetUserAsync(string subject, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        using var request = CreateAdminRequest(HttpMethod.Get, $"{AdminUsersEndpointPath}/{Uri.EscapeDataString(subject)}");
        using var response = await SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw MapFailure(response, "provider get identity");
        }

        var user = await ReadUserAsync(response, cancellationToken);
        if (string.IsNullOrWhiteSpace(user?.Id) || string.IsNullOrWhiteSpace(user.Email))
        {
            return null;
        }

        return new ProviderUser(user.Id, user.Email);
    }

    /// <inheritdoc />
    public async Task<ProviderUser?> FindUserByEmailAsync(string carrierEmail, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(carrierEmail);

        // No undocumented email filter is ever used: the documented paginated listing is
        // scanned and the normalized carrier email is exact-matched server-side.
        var normalized = carrierEmail.Trim().ToLowerInvariant();
        string? foundSubject = null;
        string? foundEmail = null;
        var listingExhausted = false;

        for (var page = 1; page <= MaxLookupPages; page++)
        {
            using var request = CreateAdminRequest(
                HttpMethod.Get,
                $"{AdminUsersEndpointPath}?page={page}&per_page={ListPageSize}");
            using var response = await SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw MapFailure(response, "provider list identities");
            }

            JsonElement body;
            try
            {
                body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            }
            catch (Exception exception)
            {
                throw new ProviderUserOperationException(
                    ProviderUserOperationFailure.Error,
                    "Provider list response was not readable JSON.",
                    exception);
            }

            if (!body.TryGetProperty("users", out var users)
                || users.ValueKind != JsonValueKind.Array)
            {
                throw new ProviderUserOperationException(
                    ProviderUserOperationFailure.Error,
                    "Provider list response had an unexpected shape (no 'users' array).");
            }

            var pageCount = 0;
            foreach (var item in users.EnumerateArray())
            {
                pageCount++;
                var email = item.TryGetProperty("email", out var emailProperty) ? emailProperty.GetString() : null;
                var id = item.TryGetProperty("id", out var idProperty) ? idProperty.GetString() : null;

                if (!string.Equals(
                        email?.Trim().ToLowerInvariant(),
                        normalized,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (foundSubject is not null)
                {
                    // More than one exact match: provider inconsistency, fail closed.
                    throw new ProviderUserOperationException(
                        ProviderUserOperationFailure.Error,
                        "Provider inconsistency: more than one identity matches the carrier email; failing closed.");
                }

                foundSubject = id;
                foundEmail = email;
            }

            if (pageCount < ListPageSize)
            {
                listingExhausted = true;
                break;
            }
        }

        if (!listingExhausted)
        {
            // The page cap was hit on a full page: the listing is not provably exhausted, so
            // "zero matches" cannot be honestly concluded — fail closed.
            throw new ProviderUserOperationException(
                ProviderUserOperationFailure.Error,
                "Provider list could not be exhausted within the page cap; failing closed.");
        }

        return foundSubject is null ? null : new ProviderUser(foundSubject, foundEmail!);
    }

    /// <inheritdoc />
    public async Task DeleteUserAsync(string subject, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        using var request = CreateAdminRequest(
            HttpMethod.Delete,
            $"{AdminUsersEndpointPath}/{Uri.EscapeDataString(subject)}");
        using var response = await SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            // Already absent: idempotent success for the delete flow.
            _logger.LogInformation("Provider identity already absent during delete; treated as success.");
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw MapFailure(response, "provider delete identity");
        }
    }

    /// <inheritdoc />
    public async Task UpdateUserEmailAsync(string subject, string newCarrierEmail, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(newCarrierEmail);

        using var request = CreateAdminRequest(
            HttpMethod.Put,
            $"{AdminUsersEndpointPath}/{Uri.EscapeDataString(subject)}");
        request.Content = JsonContent.Create(new
        {
            email = newCarrierEmail,
            email_confirm = true,
        });

        using var response = await SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw MapFailure(response, "provider update email");
        }
    }

    /// <inheritdoc />
    public async Task InitiatePasswordRecoveryAsync(string carrierEmail, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(carrierEmail);

        // Public recovery path: only the publishable key travels here; the service-role
        // secret is never attached to this request. Anti-enumeration is provider-side
        // (unknown email -> 200 {} without an email); the service has already proven the
        // account exists.
        using var request = new HttpRequestMessage(HttpMethod.Post, RecoverEndpointPath);
        request.Headers.TryAddWithoutValidation("apikey", _options.PublishableKey);
        request.Content = JsonContent.Create(new { email = carrierEmail });

        using var response = await SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw MapFailure(response, "provider password recovery");
        }
    }

    // -----------------------------------------------------------------------------------

    private HttpRequestMessage CreateAdminRequest(HttpMethod method, string pathWithQuery)
    {
        var request = new HttpRequestMessage(method, pathWithQuery);
        request.Headers.TryAddWithoutValidation("apikey", _options.PublishableKey);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            _adminOptions.ServiceRoleKey);
        return request;
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Supabase Admin transport failure (no secrets are ever logged).");
            throw new ProviderUserOperationException(
                ProviderUserOperationFailure.Unavailable,
                "Supabase Admin transport failure.",
                exception);
        }
    }

    private static async Task<UserPayload?> ReadUserAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<UserPayload>(cancellationToken);
        }
        catch (Exception exception)
        {
            throw new ProviderUserOperationException(
                ProviderUserOperationFailure.Error,
                "Provider identity response was not readable JSON.",
                exception);
        }
    }

    private ProviderUserOperationException MapFailure(
        HttpResponseMessage response,
        string operation)
    {
        var statusCode = (int)response.StatusCode;

        if (statusCode == (int)HttpStatusCode.UnprocessableEntity)
        {
            return new ProviderUserOperationException(
                ProviderUserOperationFailure.EmailAlreadyInUse,
                $"{operation}: provider rejected the carrier email as already in use (422).");
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning("Supabase Admin rate-limited (HTTP 429) during {Operation}.", operation);
            return new ProviderUserOperationException(
                ProviderUserOperationFailure.Unavailable,
                $"{operation}: provider rate-limited (429).");
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new ProviderUserOperationException(
                ProviderUserOperationFailure.UserNotFound,
                $"{operation}: provider identity not found (404).");
        }

        if (statusCode >= 500)
        {
            _logger.LogWarning("Supabase Admin server error (HTTP {StatusCode}) during {Operation}.", statusCode, operation);
            return new ProviderUserOperationException(
                ProviderUserOperationFailure.Error,
                $"{operation}: provider server error ({statusCode}).");
        }

        _logger.LogWarning(
            "Supabase Admin returned an unexpected client error (HTTP {StatusCode}) during {Operation}.",
            statusCode,
            operation);
        return new ProviderUserOperationException(
            ProviderUserOperationFailure.Error,
            $"{operation}: unexpected provider client error ({statusCode}).");
    }

    /// <summary>Minimal shape of the GoTrue user object (fields actually consumed).</summary>
    private sealed class UserPayload
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }
}