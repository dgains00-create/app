namespace DMO.Application.Persistence;

/// <summary>
/// The typed persistence failure reason of the P2-T05 repositories.
/// </summary>
/// <remarks>
/// Repository write failures are mapped from PostgreSQL constraint violations
/// (<c>PostgresException.SqlState</c> + constraint name) onto these typed reasons
/// (P2-T05 contract §20.2 binding rules) so a validator-passing input combination can never surface
/// as a generic 500.
/// </remarks>
public enum ControloPersistenceFailureReason
{
    /// <summary>A computed per-row result violates the <c>&gt; 0</c> CHECK backstop (SQLSTATE 23514).</summary>
    ResultNonPositive,

    /// <summary>A dependent row still references the target (RESTRICT FK, SQLSTATE 23503).</summary>
    DependencyExists,

    /// <summary>A named email list already owns the name (<c>email_lists_name_key</c>).</summary>
    DuplicateListName,

    /// <summary>A named email template already owns the name (<c>email_templates_name_key</c>).</summary>
    DuplicateTemplateName,

    /// <summary>The supplied repairer id does not exist in the register.</summary>
    RepairerNotFound,

    /// <summary>An address appears twice in one list (<c>email_list_recipients_list_address_key</c>).</summary>
    DuplicateAddress,

    /// <summary>The supplied production anchor <c>cm_id</c> does not exist (FK backstop).</summary>
    CmContextNotFound,

    /// <summary>The supplied pending anchor <c>tool_id</c> does not exist (FK backstop).</summary>
    ToolNotFound,
}

/// <summary>
/// The typed persistence failure of the P2-T05 repositories, mapped by the services onto the closed
/// result vocabulary.
/// </summary>
public sealed class ControloPersistenceException : Exception
{
    /// <summary>Creates the typed failure.</summary>
    public ControloPersistenceException(ControloPersistenceFailureReason reason, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Reason = reason;
    }

    /// <summary>The typed failure reason.</summary>
    public ControloPersistenceFailureReason Reason { get; }
}