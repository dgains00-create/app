namespace DMO.Application.Persistence;

/// <summary>
/// Typed reason for a Job On persistence failure.
/// </summary>
public enum JobOnPersistenceFailureReason
{
    /// <summary>Another Job On already owns the (<c>reference</c>, <c>production_number</c>) pair.</summary>
    DuplicateProduction,

    /// <summary>A supplied context Tool identity does not exist in the canonical registry.</summary>
    ToolNotFound,

    /// <summary>A supplied context Tool's type does not match the slot's context type.</summary>
    ToolTypeMismatch,

    /// <summary>A dependent operational fact still references the row (fail closed, no cascade).</summary>
    DependencyExists,
}

/// <summary>
/// Typed persistence failure raised by the Job On repository for writes that violate database
/// integrity.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §5.5/§6.4 (uniqueness), §7.4 (<c>TOOL_TYPE_MISMATCH</c>), §11.4/§11.5
/// (dependency refusal) and §12.2 rule 5.
/// <para>
/// Contextual Tool resolution happens inside the write transaction, so a concurrent change cannot
/// bypass it; the typed failure is how that in-transaction resolution reaches the service, which
/// maps it onto the contracted validation codes and refusal reasons.
/// </para>
/// </remarks>
public sealed class JobOnPersistenceException : Exception
{
    /// <summary>Creates the failure with its typed reason.</summary>
    public JobOnPersistenceException(
        JobOnPersistenceFailureReason reason,
        string message,
        Guid? existingJobOnId = null)
        : base(message)
    {
        Reason = reason;
        ExistingJobOnId = existingJobOnId;
    }

    /// <summary>Creates the failure with its typed reason, message and underlying cause.</summary>
    public JobOnPersistenceException(
        JobOnPersistenceFailureReason reason,
        string message,
        Exception innerException,
        Guid? existingJobOnId = null)
        : base(message, innerException)
    {
        Reason = reason;
        ExistingJobOnId = existingJobOnId;
    }

    /// <summary>The typed persistence failure reason.</summary>
    public JobOnPersistenceFailureReason Reason { get; }

    /// <summary>The existing Job On identity, when the failure names one.</summary>
    public Guid? ExistingJobOnId { get; }
}
