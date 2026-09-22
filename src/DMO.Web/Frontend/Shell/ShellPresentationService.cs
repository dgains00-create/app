using DMO.Application.Session;

namespace DMO.Web.Frontend.Shell;

public sealed class ShellPresentationService
{
    private readonly NavigationProjectionService _navigation;

    public ShellPresentationService(NavigationProjectionService navigation)
    {
        _navigation = navigation;
    }

    public async Task<ShellPresentation> BuildAsync(
        CurrentAccount current,
        string pageTitle,
        string pageContext,
        CancellationToken cancellationToken)
    {
        var identity = current switch
        {
            CurrentAccount.Admin(var account) => new IdentityPresentation(
                account.DisplayName, "Administrador", account.Email, null),
            CurrentAccount.User(var account) => new IdentityPresentation(
                account.DisplayName, "Utilizador", account.CompanyNumber, account.RoleLabel),
            _ => throw new InvalidOperationException("The operational shell requires a current account."),
        };

        var navigation = await _navigation.ProjectAsync(current, cancellationToken);
        var status = navigation.AccessResolutionFailed
            ? "A navegação operacional está indisponível. O acesso continua fechado."
            : "Aplicação pronta.";

        return new ShellPresentation(identity, navigation, pageTitle, pageContext, [], status);
    }
}
