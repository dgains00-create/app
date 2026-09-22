namespace DMO.Application.Accounts;

/// <summary>
/// The fixed, code-owned application UUID of the single ADMIN row.
/// </summary>
/// <remarks>
/// <para>
/// Value: <c>00000000-0000-4000-8000-0000000000ad</c> — the exact constant referenced by the
/// <c>admin_accounts_singleton_id_check</c> CHECK constraint and by the
/// <c>admin_accounts</c> primary key. The database enforces the single-ADMIN invariant with
/// two complementary constraints: the CHECK forces every <c>admin_accounts</c> row to use
/// exactly this UUID, and the PK permits that UUID only once.
/// </para>
/// <para>
/// The value is never derived from the ADMIN email, never operator-supplied and identical in
/// every environment and on every run. The ADMIN bootstrap always inserts with this constant.
/// </para>
/// </remarks>
public static class AdminAccountId
{
    /// <summary>The fixed singleton ADMIN row identifier.</summary>
    public static readonly Guid Value = Guid.Parse("00000000-0000-4000-8000-0000000000ad");
}