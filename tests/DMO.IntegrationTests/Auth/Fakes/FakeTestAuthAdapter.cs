using DMO.Application.Authentication;

namespace DMO.IntegrationTests.Auth.Fakes;

/// <summary>
/// Test-host-only fake authentication boundary used by the auth endpoint plumbing tests.
/// </summary>
/// <remarks>
/// Registered only through <c>ConfigureTestServices</c> inside the test host; production
/// composition keeps the real <see cref="DMO.Web.Auth.SupabaseAuthenticationService"/>.
/// The fake echoes the accepted request shapes and never invents access facts.
/// </remarks>
public sealed class FakeTestAuthAdapter : IAuthenticationBoundary
{
    /// <inheritdoc />
    public Task<AuthenticationOutcome> AuthenticateAsync(
        AuthenticationRequest request,
        CancellationToken cancellationToken)
    {
        string subject;
        AuthenticationPath path;

        switch (request)
        {
            case AdminLoginRequest:
                subject = "test-admin-subject";
                path = AuthenticationPath.Admin;
                break;
            case UserLoginRequest:
                subject = "test-user-subject";
                path = AuthenticationPath.User;
                break;
            default:
                return Task.FromResult<AuthenticationOutcome>(
                    new AuthenticationOutcome.Failed(AuthenticationFailureReason.InvalidCredentials));
        }

        return Task.FromResult<AuthenticationOutcome>(
            new AuthenticationOutcome.Authenticated(new AuthenticatedIdentity(subject, path)));
    }
}