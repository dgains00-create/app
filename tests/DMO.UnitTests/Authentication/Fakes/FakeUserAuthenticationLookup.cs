using DMO.Application.Authentication;

namespace DMO.UnitTests.Authentication.Fakes;

/// <summary>
/// Test-only <see cref="IUserAuthenticationLookup"/> with configurable results.
/// </summary>
/// <remarks>
/// Supplies the narrow persistence seam only — the service under test is the real product
/// logic. Records the company numbers observed so tests can assert that an unknown company
/// number never reaches the provider and that the identifier is never transformed.
/// </remarks>
public sealed class FakeUserAuthenticationLookup : IUserAuthenticationLookup
{
    private readonly Func<string, UserLoginIdentity?> _lookup;

    /// <summary>Creates a lookup returning the same identity for every company number.</summary>
    public FakeUserAuthenticationLookup(UserLoginIdentity? identity)
    {
        _lookup = _ => identity;
    }

    /// <summary>Creates a lookup whose result is derived from the company number.</summary>
    public FakeUserAuthenticationLookup(Func<string, UserLoginIdentity?> lookup)
    {
        ArgumentNullException.ThrowIfNull(lookup);
        _lookup = lookup;
    }

    /// <summary>Creates a lookup that always throws.</summary>
    public static FakeUserAuthenticationLookup Throwing(Exception exception) =>
        new(_ => throw exception);

    /// <summary>Company numbers observed by this lookup, in order.</summary>
    public List<string> CompanyNumbers { get; } = [];

    /// <inheritdoc />
    public Task<UserLoginIdentity?> GetByCompanyNumberAsync(
        string companyNumber,
        CancellationToken cancellationToken)
    {
        CompanyNumbers.Add(companyNumber);
        return Task.FromResult(_lookup(companyNumber));
    }
}