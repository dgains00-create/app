using DMO.Application.Authentication;

namespace DMO.UnitTests.Authentication.Fakes;

/// <summary>
/// Test-only fake authentication boundary for the ADMIN credential contract.
/// </summary>
/// <remarks>
/// Fakes live only in test files and are never referenced by production composition. This
/// adapter echoes the ADMIN request shape so contract tests can verify the boundary's
/// outcome surface without any provider.
/// </remarks>
public sealed class FakeAdminAuthAdapter : IAuthenticationBoundary
{
    /// <inheritdoc />
    public Task<AuthenticationOutcome> AuthenticateAsync(
        AuthenticationRequest request,
        CancellationToken cancellationToken)
    {
        if (request is not AdminLoginRequest admin || string.IsNullOrWhiteSpace(admin.Password))
        {
            return Task.FromResult<AuthenticationOutcome>(
                new AuthenticationOutcome.Failed(AuthenticationFailureReason.InvalidCredentials));
        }

        return Task.FromResult<AuthenticationOutcome>(
            new AuthenticationOutcome.Authenticated(
                new AuthenticatedIdentity($"admin:{admin.Email}", AuthenticationPath.Admin)));
    }
}