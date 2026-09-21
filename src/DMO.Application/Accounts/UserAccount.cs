namespace DMO.Application.Accounts;

/// <summary>
/// Application USER account record produced by account resolution.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CompanyNumber"/> is the canonical, presented-to-the-USER login identifier.
/// Email is stored independently and is never a USER login identifier.
/// </para>
/// <para>
/// <see cref="RoleLabel"/> is free text for presentation only and never grants access.
/// </para>
/// <para>
/// There is no Template field and no Template/access state in P1-T02. Template resolution is
/// a P1-T03/P1-T04 invariant.
/// </para>
/// </remarks>
/// <param name="AccountId">Stable application account identity.</param>
/// <param name="CompanyNumber">Canonical USER login identifier.</param>
/// <param name="DisplayName">Human-readable display name.</param>
/// <param name="Email">Independent stored email; not the normal USER login identifier.</param>
/// <param name="RoleLabel">Presentation-only free-text role label.</param>
/// <param name="IsActive">Whether the account is active.</param>
public sealed record UserAccount(
    Guid AccountId,
    string CompanyNumber,
    string DisplayName,
    string Email,
    string RoleLabel,
    bool IsActive);