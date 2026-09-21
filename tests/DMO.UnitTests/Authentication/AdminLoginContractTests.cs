using System.Reflection;
using DMO.Application.Authentication;

namespace DMO.UnitTests.Authentication;

/// <summary>
/// P1-T02 tests — the ADMIN login contract is <c>email + password</c>; the ADMIN email is the
/// dedicated ADMIN login identifier and no other identifier is invented.
/// </summary>
public sealed class AdminLoginContractTests
{
    [Fact]
    public void AdminLoginRequest_IsEmailBased()
    {
        // Preconditions: the contract type itself.
        var requestType = typeof(AdminLoginRequest);

        // Assertions: the email is the login identifier.
        Assert.NotNull(requestType.GetProperty("Email", BindingFlags.Public | BindingFlags.Instance));
        Assert.NotNull(requestType.GetProperty("Password", BindingFlags.Public | BindingFlags.Instance));
    }

    [Fact]
    public void AdminLoginRequest_HasNoAdminLoginName()
    {
        // Required non-effect: no proxy ADMIN identifier abstraction exists.
        var requestType = typeof(AdminLoginRequest);

        Assert.Null(requestType.GetProperty("AdminLoginName", BindingFlags.Public | BindingFlags.Instance));
        Assert.Null(requestType.GetProperty("LoginName", BindingFlags.Public | BindingFlags.Instance));
    }

    [Fact]
    public void AdminLoginRequest_CarriesEmailAndPassword()
    {
        var request = new AdminLoginRequest("admin@dmo.test", "secret");

        Assert.Equal("admin@dmo.test", request.Email);
        Assert.Equal("secret", request.Password);
    }
}