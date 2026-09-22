using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DMO.Application.Authentication;
using Microsoft.Extensions.Options;

namespace DMO.Web.Auth;

/// <summary>
/// Production <see cref="IAuthenticationBoundary"/> — the single production authentication
/// boundary for both the ADMIN and USER flows.
/// </summary>
/// <remarks>
/// <para>
/// <b>ADMIN path</b> (<see cref="AdminLoginRequest"/>): real Supabase Auth against the
/// DEV/TEST project, using only the project URL and the publishable key. No
/// <c>service_role</c> and no secret key is used or stored.
/// </para>
/// <para>
/// <b>USER path</b> (<see cref="UserLoginRequest"/>): the USER types
/// <c>company_number + password</c> only. The boundary resolves the account's provisioned
/// carrier email for the company number through the narrow
/// <see cref="IUserAuthenticationLookup"/> persistence contract (never EF directly), then
/// performs the real Supabase password grant with that carrier email. The company number is
/// never converted into an email, never sent to the provider as an email, and no synthetic
/// email and no local password store exist.
/// </para>
/// <para>
/// Unknown company number → <see cref="AuthenticationFailureReason.InvalidCredentials"/>
/// with <b>no provider round trip</b>.
/// </para>
/// <para>
/// <b>Transport</b> (verified against the current official Supabase documentation and the
/// current official client sources before implementation):
/// </para>
/// <code>
/// password grant : POST {ProjectUrl}/auth/v1/token?grant_type=password
///                  body { email, password }
/// key header     : the publishable key travels ONLY in the `apikey` header. Current
///                  publishable keys (sb_publishable_…) are never sent as Bearer tokens.
/// verification   : GET {ProjectUrl}/auth/v1/user with
///                  `Authorization: Bearer &lt;access token&gt;` (plus `apikey`)
/// errors         : 400/401 -> InvalidCredentials; 429 -> ProviderUnavailable;
///                  5xx and other unexpected 4xx (403/404/...) -> ProviderError;
///                  transport failure -> ProviderUnavailable
/// </code>
/// <para>
/// The access token returned by Supabase lives only inside this call and is never persisted,
/// never logged and never enters <see cref="DMO.Application"/>.
/// </para>
/// </remarks>
public sealed class SupabaseAuthenticationService : IAuthenticationBoundary
{
    /// <summary>GoTrue password-grant token endpoint, relative to the project URL.</summary>
    public const string TokenEndpointPath = "auth/v1/token?grant_type=password";

    /// <summary>GoTrue authenticated-user endpoint, relative to the project URL.</summary>
    public const string UserEndpointPath = "auth/v1/user";

    private readonly HttpClient _httpClient;
    private readonly SupabaseOptions _options;
    private readonly ILogger<SupabaseAuthenticationService> _logger;
    private readonly IUserAuthenticationLookup _userLookup;

    /// <summary>Creates the service over the configured HTTP client, options and USER lookup.</summary>
    public SupabaseAuthenticationService(
        HttpClient httpClient,
        IOptions<SupabaseOptions> options,
        ILogger<SupabaseAuthenticationService> logger,
        IUserAuthenticationLookup userLookup)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(userLookup);

        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _userLookup = userLookup;
    }

    /// <inheritdoc />
    public async Task<AuthenticationOutcome> AuthenticateAsync(
        AuthenticationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request switch
        {
            AdminLoginRequest admin => await AuthenticateAdminAsync(admin, cancellationToken),
            UserLoginRequest user => await AuthenticateUserAsync(user, cancellationToken),
            _ => Failed(AuthenticationFailureReason.ProviderUnavailable),
        };
    }

    /// <summary>
    /// Performs the shared GoTrue password-grant + user-verification exchange and produces
    /// the minimal authenticated identity on the given path.
    /// </summary>
    /// <remarks>
    /// The <paramref name="email"/> parameter is the provider credential carrier: for ADMIN
    /// it is the presented ADMIN email; for USER it is the persisted carrier email resolved
    /// from the company number. The company number itself is never sent here.
    /// </remarks>
    private async Task<AuthenticationOutcome> ExchangeGrantAsync(
        string email,
        string password,
        AuthenticationPath path,
        CancellationToken cancellationToken)
    {
        TokenResponse? tokenResponse;
        try
        {
            using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, TokenEndpointPath);
            tokenRequest.Headers.TryAddWithoutValidation("apikey", _options.PublishableKey);
            tokenRequest.Content = JsonContent.Create(new { email, password });

            using var tokenHttpResponse = await _httpClient.SendAsync(tokenRequest, cancellationToken);

            if (!tokenHttpResponse.IsSuccessStatusCode)
            {
                return MapHttpFailure(tokenHttpResponse.StatusCode);
            }

            tokenResponse = await tokenHttpResponse.Content.ReadFromJsonAsync<TokenResponse>(
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Supabase Auth transport failure during the password grant.");
            return Failed(AuthenticationFailureReason.ProviderUnavailable);
        }

        if (string.IsNullOrWhiteSpace(tokenResponse?.AccessToken))
        {
            _logger.LogWarning("Supabase Auth returned a success response without an access token.");
            return Failed(AuthenticationFailureReason.ProviderError);
        }

        // Verify the identity through the authenticated user endpoint before producing an
        // identity. The access token is used transiently in-memory only.
        string? subject;
        try
        {
            using var verifyRequest = new HttpRequestMessage(HttpMethod.Get, UserEndpointPath);
            verifyRequest.Headers.TryAddWithoutValidation("apikey", _options.PublishableKey);
            verifyRequest.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);

            using var verifyHttpResponse = await _httpClient.SendAsync(verifyRequest, cancellationToken);

            if (!verifyHttpResponse.IsSuccessStatusCode)
            {
                return MapHttpFailure(verifyHttpResponse.StatusCode);
            }

            var user = await verifyHttpResponse.Content.ReadFromJsonAsync<UserResponse>(
                cancellationToken: cancellationToken);
            subject = user?.Id;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Supabase Auth transport failure during user verification.");
            return Failed(AuthenticationFailureReason.ProviderUnavailable);
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            _logger.LogWarning("Supabase Auth could not confirm the authenticated user identity.");
            return Failed(AuthenticationFailureReason.ProviderError);
        }

        // Internal linkage only: the subject plus the mechanical path. No email, no claims,
        // no token enter the application.
        return new AuthenticationOutcome.Authenticated(
            new AuthenticatedIdentity(subject, path));
    }

    private async Task<AuthenticationOutcome> AuthenticateAdminAsync(
        AdminLoginRequest request,
        CancellationToken cancellationToken)
    {
        // A blank credential can never authenticate; do not send it to the provider.
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Failed(AuthenticationFailureReason.InvalidCredentials);
        }

        return await ExchangeGrantAsync(
            request.Email,
            request.Password,
            AuthenticationPath.Admin,
            cancellationToken);
    }

    private async Task<AuthenticationOutcome> AuthenticateUserAsync(
        UserLoginRequest request,
        CancellationToken cancellationToken)
    {
        // A blank company number/password can never authenticate; do not send it to the
        // provider and do not touch persistence.
        if (string.IsNullOrWhiteSpace(request.CompanyNumber) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Failed(AuthenticationFailureReason.InvalidCredentials);
        }

        // 1. company_number -> persisted carrier email (narrow persistence contract; unique
        // exact match; no provider call yet). The company number is never converted into an
        // email and never sent to the provider as an email.
        UserLoginIdentity? loginIdentity;
        try
        {
            loginIdentity = await _userLookup.GetByCompanyNumberAsync(
                request.CompanyNumber,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "USER authentication lookup failed for company number; treated as provider-unavailable.");
            return Failed(AuthenticationFailureReason.ProviderUnavailable);
        }

        // 2. Unknown company number -> InvalidCredentials, with no provider round trip.
        if (loginIdentity is null || string.IsNullOrWhiteSpace(loginIdentity.CarrierEmail))
        {
            return Failed(AuthenticationFailureReason.InvalidCredentials);
        }

        // 3. Real Supabase password grant with the carrier email + the presented password.
        return await ExchangeGrantAsync(
            loginIdentity.CarrierEmail,
            request.Password,
            AuthenticationPath.User,
            cancellationToken);
    }

    private AuthenticationOutcome MapHttpFailure(HttpStatusCode statusCode)
    {
        var code = (int)statusCode;

        if (code >= 500)
        {
            _logger.LogWarning("Supabase Auth server error (HTTP {StatusCode}).", code);
            return Failed(AuthenticationFailureReason.ProviderError);
        }

        if (statusCode == HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning("Supabase Auth rate-limited (HTTP 429).");
            return Failed(AuthenticationFailureReason.ProviderUnavailable);
        }

        if (statusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
        {
            // HTTP 400 / 401 — the provider rejected the presented credentials.
            _logger.LogInformation("Supabase Auth rejected the presented credentials (HTTP {StatusCode}).", code);
            return Failed(AuthenticationFailureReason.InvalidCredentials);
        }

        // Any other unexpected 4xx (403, 404, ...) is a provider/protocol/configuration fact,
        // never a human-credential fact.
        _logger.LogWarning("Supabase Auth returned an unexpected client error (HTTP {StatusCode}).", code);
        return Failed(AuthenticationFailureReason.ProviderError);
    }

    private static AuthenticationOutcome Failed(AuthenticationFailureReason reason) =>
        new AuthenticationOutcome.Failed(reason);

    /// <summary>Minimal shape of the GoTrue token response (fields actually consumed).</summary>
    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }

    /// <summary>Minimal shape of the GoTrue user response (fields actually consumed).</summary>
    private sealed class UserResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }
}