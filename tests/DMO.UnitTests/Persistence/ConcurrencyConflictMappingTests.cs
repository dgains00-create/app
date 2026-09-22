using DMO.Application.Persistence;
using DMO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update;

namespace DMO.UnitTests.Persistence;

/// <summary>
/// P1-T03 tests — the EF <see cref="DbUpdateConcurrencyException"/> maps to the domain typed
/// <see cref="ConcurrencyConflictException"/>; other exceptions are not mapped.
/// </summary>
public sealed class ConcurrencyConflictMappingTests
{
    [Fact]
    public void DbUpdateConcurrencyException_MapsToDomainConflict()
    {
        // Preconditions: EF reports a concurrency failure (stale version write).
        var inner = new DbUpdateConcurrencyException(
            "Database operation expected to affect 1 row(s) but actually affected 0 row(s).",
            Array.Empty<IUpdateEntry>());

        // Action: map through the persistence helper.
        var conflict = ConcurrencyConflictExceptionMapping.ToDomainConflict(inner);

        // Assertions: exact domain type, same inner cause, and an operator-facing message
        // that states no silent overwrite happened.
        Assert.IsType<ConcurrencyConflictException>(conflict);
        Assert.Same(inner, conflict.InnerException);
        Assert.Contains("concurrent", conflict.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("overwrit", conflict.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DomainConflict_IsTypedAndActionable()
    {
        // The caller can distinguish a concurrency conflict (409) from other failures by type.
        var conflict = new ConcurrencyConflictException("stale version", new InvalidOperationException("root"));

        Assert.IsAssignableFrom<InvalidOperationException>(conflict);
        Assert.IsType<ConcurrencyConflictException>(conflict);
        Assert.Equal("stale version", conflict.Message);
        Assert.Equal("root", conflict.InnerException?.Message);
    }
}