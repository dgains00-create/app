using System.Reflection;
using DMO.Application.Authentication;

namespace DMO.UnitTests.Authentication;

/// <summary>
/// P1-T02 tests — the USER login contract remains <c>company_number + password</c>; email is
/// never a USER login identifier.
/// </summary>
public sealed class UserLoginContractTests
{
    [Fact]
    public void UserLoginRequest_DoesNotAcceptEmailAsIdentifier()
    {
        // Preconditions: the contract type itself.
        var requestType = typeof(UserLoginRequest);

        // Assertions: there is no email field and no alternate canonical login identifier.
        Assert.Null(requestType.GetProperty("Email", BindingFlags.Public | BindingFlags.Instance));
        Assert.NotNull(requestType.GetProperty("CompanyNumber", BindingFlags.Public | BindingFlags.Instance));
        Assert.NotNull(requestType.GetProperty("Password", BindingFlags.Public | BindingFlags.Instance));
    }

    [Fact]
    public void UserLoginRequest_IsCompanyNumberBased()
    {
        // Preconditions: document the settled shape by construction.
        var request = new UserLoginRequest("2661", "secret");

        // Assertions: the company number is the login identifier.
        Assert.Equal("2661", request.CompanyNumber);
        Assert.Equal("secret", request.Password);
    }

    [Fact]
    public void CompanyNumber_IsNeverTransformedIntoAnEmail()
    {
        // Preconditions: the runtime request shape is the only transport into the boundary.
        var request = new UserLoginRequest("2661", "secret");

        // Required non-effect: no synthetic email exists on the request, so a USER request
        // can never be turned into a Supabase email login by the boundary.
        Assert.DoesNotContain("@", request.CompanyNumber, StringComparison.Ordinal);
        Assert.Null(requestType().GetProperty("Email"));
    }

    private static Type requestType() => typeof(UserLoginRequest);
}