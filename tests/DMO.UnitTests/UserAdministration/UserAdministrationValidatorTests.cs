using DMO.Application.UserAdministration;

namespace DMO.UnitTests.UserAdministration;

/// <summary>
/// Server-side validation rules of the USER administration commands (P1-T05 §16): pure
/// static validation of application facts — no password validation exists anywhere because
/// no password exists in the invite flow.
/// </summary>
public sealed class UserAdministrationValidatorTests
{
    [Fact]
    public void Create_ValidCommand_NoErrors()
    {
        var errors = UserAdministrationValidator.Validate(ValidCommand());

        Assert.Empty(errors);
    }

    [Fact]
    public void Create_BlankFacts_ReportsEachError()
    {
        var errors = UserAdministrationValidator.Validate(
            ValidCommand() with { Name = " ", CompanyNumber = "", Email = "  " });

        Assert.Contains(errors, error => error.Contains("Name", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("Company number", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("Email", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@dominio.pt")]
    [InlineData("user@")]
    [InlineData("us er@dominio.pt")]
    [InlineData("user@dom inio.pt")]
    [InlineData("user@dominio.pt ")]
    public void Create_InvalidEmails_ReportError(string email)
    {
        var errors = UserAdministrationValidator.Validate(ValidCommand() with { Email = email });

        Assert.Contains(errors, error => error.Contains("Email", StringComparison.Ordinal));
    }

    [Fact]
    public void Create_EmailOverMaxLength_ReportError()
    {
        var email = $"{new string('a', 64)}@{new string('b', 255)}.pt";

        Assert.False(UserAdministrationValidator.IsValidProviderCompatibleEmail(email));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("https://app.dmo.pt/setup")]
    [InlineData("http://localhost:5000/setup")]
    public void Create_RedirectUrl_NoneOrAbsoluteHttp_NoError(string? redirectUrl)
    {
        var errors = UserAdministrationValidator.Validate(
            ValidCommand() with { InviteRedirectUrl = redirectUrl });

        Assert.DoesNotContain(errors, error => error.Contains("redirect", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://app.dmo.pt")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/relative/only")]
    public void Create_InvalidRedirectUrl_ReportError(string redirectUrl)
    {
        var errors = UserAdministrationValidator.Validate(
            ValidCommand() with { InviteRedirectUrl = redirectUrl });

        Assert.Contains(errors, error => error.Contains("redirect", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Create_RoleOverMaxLength_ReportError()
    {
        var errors = UserAdministrationValidator.Validate(
            ValidCommand() with { Role = new string('r', UserAdministrationValidator.MaxRoleLength + 1) });

        Assert.Contains(errors, error => error.Contains("Role", StringComparison.Ordinal));
    }

    [Fact]
    public void Create_CompanyNumberOverMaxLength_ReportError()
    {
        var errors = UserAdministrationValidator.Validate(
            ValidCommand() with { CompanyNumber = new string('9', UserAdministrationValidator.MaxCompanyNumberLength + 1) });

        Assert.Contains(errors, error => error.Contains("Company number", StringComparison.Ordinal));
    }

    [Fact]
    public void Update_ValidCommand_NoErrors()
    {
        var errors = UserAdministrationValidator.Validate(
            new UserAdministrationCommands.UpdateUserCommand(
                Guid.NewGuid(), "João Silva", "2661", "joao@dmo.test", "Reparador", ExpectedVersion: 1));

        Assert.Empty(errors);
    }

    [Fact]
    public void Update_BlankName_ReportError()
    {
        var errors = UserAdministrationValidator.Validate(
            new UserAdministrationCommands.UpdateUserCommand(
                Guid.NewGuid(), "  ", "2661", "joao@dmo.test", "Reparador", ExpectedVersion: 1));

        Assert.Contains(errors, error => error.Contains("Name", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("a@b")]
    [InlineData("novo@dmo.test")]
    public void ProviderCompatibleEmail_AcceptsRfcShape_WithoutInventingRules(string email)
    {
        Assert.True(UserAdministrationValidator.IsValidProviderCompatibleEmail(email));
    }

    [Fact]
    public void SimpleStateCommands_RequireNonNullCommand()
    {
        Assert.Throws<ArgumentNullException>(() => UserAdministrationValidator.Validate(
            (UserAdministrationCommands.SetUserActiveCommand)null!));
        Assert.Throws<ArgumentNullException>(() => UserAdministrationValidator.Validate(
            (UserAdministrationCommands.SetUserTemplateCommand)null!));
        Assert.Throws<ArgumentNullException>(() => UserAdministrationValidator.Validate(
            (UserAdministrationCommands.DeleteUserCommand)null!));
        Assert.Throws<ArgumentNullException>(() => UserAdministrationValidator.Validate(
            (UserAdministrationCommands.RequestedPasswordResetCommand)null!));
        Assert.Throws<ArgumentNullException>(() => UserAdministrationValidator.Validate(
            (UserAdministrationCommands.ResendInviteCommand)null!));
        Assert.Throws<ArgumentNullException>(() => UserAdministrationValidator.Validate(
            (UserAdministrationCommands.CreateUserCommand)null!));
        Assert.Throws<ArgumentNullException>(() => UserAdministrationValidator.Validate(
            (UserAdministrationCommands.UpdateUserCommand)null!));
    }

    private static UserAdministrationCommands.CreateUserCommand ValidCommand() => new(
        Name: "João Silva",
        CompanyNumber: "2661",
        Email: "joao@dmo.test",
        Role: "Reparador",
        Active: true,
        TemplateId: null,
        InviteRedirectUrl: null);
}