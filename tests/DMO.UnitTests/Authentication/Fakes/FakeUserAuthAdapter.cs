using DMO.Application.Authentication;

namespace DMO.UnitTests.Authentication.Fakes;

/// <summary>
/// Test-only fake authentication boundary for the USER credential contract.
/// </summary>
/// <remarks>
/// Fakes live only in test files and are never referenced by production composition. This
/// adapter echoes the USER request shape so contract tests can verify the boundary's
/// outcome surface without any provider.
/// </remarks>
public sealed class FakeUserAuthAdapter : IAuthenticationBoundary
{
    /// <inheritdoc />
    public Task<AuthenticationOutcome> AuthenticateAsync(
        AuthenticationRequest request,
        CancellationToken cancellationToken)
    {
        if (request is not UserLoginRequest user || string.IsNullOrWhiteSpace(user.Password))
        {
            return Task.FromResult<AuthenticationOutcome>(
                new AuthenticationOutcome.Failed(AuthenticationFailureReason.InvalidCredentials));
        }

        return Task.FromResult<AuthenticationOutcome>(
            new AuthenticationOutcome.Authenticated(
                new AuthenticatedIdentity($"user:{user.CompanyNumber}", AuthenticationPath.User)));
    }
}