using DMO.Application.Authentication;

namespace DMO.IntegrationTests.Auth.Fakes;

/// <summary>
/// Test-host-only <see cref="IUserAuthenticationLookup"/> for the auth host plumbing tests.
/// </summary>
/// <remarks>
/// Registered only through <c>ConfigureTestServices</c> inside the test host; production
/// composition keeps <see cref="DMO.Infrastructure.Persistence.UserAuthenticationLookup"/>.
/// </remarks>
public sealed class FakeIntegrationUserAuthenticationLookup : IUserAuthenticationLookup
{
    /// <summary>Default carrier email used by the host plumbing tests (never committed secrets).</summary>
    public const string DefaultCarrierEmail = "carrier@dmo.test";

    private readonly UserLoginIdentity? _identity;
    private readonly List<string> _companyNumbers = [];

    /// <summary>Creates a lookup returning the given carrier identity (null = unknown company number).</summary>
    public FakeIntegrationUserAuthenticationLookup(UserLoginIdentity? identity)
    {
        _identity = identity;
    }

    /// <summary>Creates a lookup returning the default carrier email.</summary>
    public FakeIntegrationUserAuthenticationLookup()
        : this(new UserLoginIdentity(DefaultCarrierEmail))
    {
    }

    /// <summary>Company numbers observed by this lookup, in order.</summary>
    public IReadOnlyList<string> CompanyNumbers => _companyNumbers;

    /// <inheritdoc />
    public Task<UserLoginIdentity?> GetByCompanyNumberAsync(
        string companyNumber,
        CancellationToken cancellationToken)
    {
        _companyNumbers.Add(companyNumber);
        return Task.FromResult(_identity);
    }
}