namespace DMO.Application.UserAdministration;

/// <summary>
/// Kind of a privileged provider operation failure surfaced to the administration service.
/// </summary>
/// <remarks>
/// The service never sees HTTP/JSON; the adapter translates provider responses into this
/// typed failure vocabulary so Application logic stays transport-free.
/// </remarks>
public enum ProviderUserOperationFailure
{
    /// <summary>The provider could not be reached or was rate-limited (retryable).</summary>
    Unavailable,

    /// <summary>The provider rejected the operation for an unexpected reason.</summary>
    Error,

    /// <summary>The provider already holds a confirmed identity for the carrier email.</summary>
    EmailAlreadyInUse,

    /// <summary>The provider identity does not exist.</summary>
    UserNotFound,

    /// <summary>The invited identity has already confirmed its invitation.</summary>
    AlreadyConfirmed,
}

/// <summary>
/// Raised by <see cref="IUserIdentityProvisioner"/> implementations when a privileged
/// provider operation fails, carrying the typed <see cref="ProviderUserOperationFailure"/>.
/// </summary>
/// <remarks>
/// The administration service catches this exception and maps it to a closed-set
/// <see cref="UserAdministrationResult"/>; it is never surfaced raw to the transport layer.
/// </remarks>
public sealed class ProviderUserOperationException : Exception
{
    /// <summary>Creates the failure with its typed kind and an operator-facing message.</summary>
    public ProviderUserOperationException(
        ProviderUserOperationFailure failure,
        string message)
        : base(message)
    {
        Failure = failure;
    }

    /// <summary>Creates the failure with a typed kind, message and underlying cause.</summary>
    public ProviderUserOperationException(
        ProviderUserOperationFailure failure,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        Failure = failure;
    }

    /// <summary>The typed provider failure kind.</summary>
    public ProviderUserOperationFailure Failure { get; }
}