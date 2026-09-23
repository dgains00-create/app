namespace DMO.Application.JobOn;

/// <summary>
/// One contributing module's answer to "does anything depend on this Job On?".
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §11.5 and §12.4.
/// <para>
/// A probe reports a <b>fact</b>, never a permission and never a rule about another module's data.
/// The Job On module never references a Controlo/Boquilhas/document type: this interface is the
/// extension seam, and later workstreams supply their own implementations through one additive
/// registration line each.
/// </para>
/// </remarks>
public interface IJobOnDependencyProbe
{
    /// <summary>Inspects the target and reports the dependencies this module owns.</summary>
    Task<JobOnDependencyReport> InspectAsync(
        JobOnDependencyTarget target,
        CancellationToken cancellationToken);
}

/// <summary>The Job On occurrence and its context identities offered to every registered probe.</summary>
public sealed record JobOnDependencyTarget(
    Guid JobOnId,
    Guid? CmContextId,
    Guid? MfContextId,
    Guid? BqContextId);

/// <summary>One contributing module's dependency report.</summary>
public sealed record JobOnDependencyReport(string Source, IReadOnlyList<JobOnDependency> Dependencies)
{
    /// <summary>The honest "nothing depends on it" report of a source.</summary>
    public static JobOnDependencyReport None(string source) => new(source, []);

    /// <summary>Whether the source reported at least one dependency.</summary>
    public bool HasDependencies => Dependencies.Count > 0;
}

/// <summary>One reported dependency: its kind plus a human description.</summary>
public sealed record JobOnDependency(string Kind, string Description);
