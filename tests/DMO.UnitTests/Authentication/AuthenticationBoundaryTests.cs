using DMO.Application.Authentication;
using DMO.UnitTests.Authentication.Fakes;

namespace DMO.UnitTests.Authentication;

/// <summary>
/// P1-T02 tests — the authentication boundary accepts the two settled credential contracts
/// and expresses only provider/credential facts.
/// </summary>
/// <remarks>
/// The boundary is exercised through test-only fake adapters; the assertions target the real
/// request/outcome contract shapes, not the fake's internals.
/// </remarks>
public sealed class AuthenticationBoundaryTests
{
    private readonly FakeAdminAuthAdapter _adminAdapter = new();
    private readonly FakeUserAuthAdapter _userAdapter = new();

    [Fact]
    public async Task Authenticate_AdminEmailPassword_ReturnsAuthenticatedIdentity()
    {
        // Preconditions: a valid ADMIN login request (email + password).
        var request = new AdminLoginRequest("admin@dmo.test", "correct-password");

        // Action: authenticate through the boundary.
        var outcome = await _adminAdapter.AuthenticateAsync(request, CancellationToken.None);

        // Assertions: an identity is established with the mechanical ADMIN path.
        var authenticated = Assert.IsType<AuthenticationOutcome.Authenticated>(outcome);
        Assert.Equal(AuthenticationPath.Admin, authenticated.Identity.AuthenticationPath);
        Assert.False(string.IsNullOrWhiteSpace(authenticated.Identity.ProviderSubject));

        // Required non-effect: the identity carries no account/access facts (no email, no
        // claims, no token — enforced by the type shape of AuthenticatedIdentity).
    }

    [Fact]
    public async Task Authenticate_UserCompanyNumberPassword_ReturnsAuthenticatedIdentity()
    {
        // Preconditions: a valid USER login request (company number + password).
        var request = new UserLoginRequest("2661", "correct-password");

        // Action: authenticate through the boundary.
        var outcome = await _userAdapter.AuthenticateAsync(request, CancellationToken.None);

        // Assertions: an identity is established with the mechanical USER path.
        var authenticated = Assert.IsType<AuthenticationOutcome.Authenticated>(outcome);
        Assert.Equal(AuthenticationPath.User, authenticated.Identity.AuthenticationPath);
        Assert.False(string.IsNullOrWhiteSpace(authenticated.Identity.ProviderSubject));
    }

    [Fact]
    public async Task AuthenticationFailure_IsCredentialProviderFactOnly()
    {
        // Preconditions: the enum is sealed to exactly the accepted provider/credential facts.
        var reasons = Enum.GetValues<AuthenticationFailureReason>();

        // Assertions: unknown/inactive/ambiguous account facts cannot be expressed here.
        Assert.Equal(
            new[]
            {
                AuthenticationFailureReason.InvalidCredentials,
                AuthenticationFailureReason.ProviderUnavailable,
                AuthenticationFailureReason.ProviderError,
            },
            reasons);
        Assert.Equal(3, reasons.Length);

        // Required non-effect: an invalid credential yields exactly the credential fact.
        var outcome = await _adminAdapter.AuthenticateAsync(
            new AdminLoginRequest("admin@dmo.test", ""), CancellationToken.None);
        var failed = Assert.IsType<AuthenticationOutcome.Failed>(outcome);
        Assert.Equal(AuthenticationFailureReason.InvalidCredentials, failed.Reason);
    }
}