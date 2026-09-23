namespace DMO.Domain.Controlo;

/// <summary>
/// One entry of the canonical repairer register.
/// </summary>
/// <remarks>
/// Authority: P2-T05 contract §10 and §16.3. The required business data is <b>name only</b>
/// (§10.1); no address, email, phone, supplier code, tax data or contact person is invented or
/// required (REP4/AC-D4). There is no unique name constraint (two repairers may share a name;
/// selection is by row, explicit — Q-REP) and no active/inactive column in P2-T05 (§10.3).
/// </remarks>
public sealed record Repairer(
    RepairerId RepairerId,
    string Name,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);