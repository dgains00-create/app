namespace DMO.Application.Accounts;

/// <summary>
/// Application ADMIN account record produced by account resolution.
/// </summary>
/// <remarks>
/// The ADMIN account has no operational Template: no Template field exists anywhere in this
/// model.
/// </remarks>
/// <param name="AccountId">Stable application account identity.</param>
/// <param name="DisplayName">Human-readable display name.</param>
/// <param name="Email">ADMIN email (the dedicated ADMIN login identifier).</param>
/// <param name="IsActive">Whether the account is active.</param>
public sealed record AdminAccount(
    Guid AccountId,
    string DisplayName,
    string Email,
    bool IsActive);