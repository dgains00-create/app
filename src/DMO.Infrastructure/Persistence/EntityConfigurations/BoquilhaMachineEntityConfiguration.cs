using DMO.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMO.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// EF configuration for the <c>boquilha_machines</c> table: the registered machine/line set of the
/// trace.
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §6.2 (exact columns/CHECK/unique). One-or-more rows per aggregate
/// (validator <c>MACHINES_REQUIRED</c>); machine codes are the settled independent
/// <c>B1..C3</c>; the set is search/filter context, never identity, with no grouping column, no
/// <c>is_primary</c> and no cascade to movements. No version column: the aggregate's version
/// protects the set (parent-protects-child precedent).</remarks>
public sealed class BoquilhaMachineEntityConfiguration : IEntityTypeConfiguration<BoquilhaMachineEntity>
{
    /// <summary>Database name of the machine-code CHECK.</summary>
    public const string MachineCheckConstraintName = "boquilha_machines_machine_check";

    /// <summary>Database name of the owning-aggregate FK.</summary>
    public const string BoquilhasForeignKeyConstraintName = "FK_boquilha_machines_boquilhas_boquilhas_id";

    /// <summary>Database name of the one-row-per-machine-per-aggregate unique key.</summary>
    public const string BoquilhasMachineUniqueConstraintName = "boquilha_machines_boquilhas_machine_key";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BoquilhaMachineEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("boquilha_machines", table =>
        {
            table.HasCheckConstraint(
                MachineCheckConstraintName,
                "machine IN ('B1','B2','B3','C1','C2','C3')");
        });

        builder.HasKey(machine => machine.BoquilhaMachineId);
        builder.Property(machine => machine.BoquilhaMachineId)
            .HasColumnName("boquilha_machine_id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(machine => machine.BoquilhasId)
            .HasColumnName("boquilhas_id")
            .IsRequired();

        builder.Property(machine => machine.Machine)
            .HasColumnName("machine")
            .IsRequired();

        builder.HasOne<BoquilhaEntity>()
            .WithMany()
            .HasForeignKey(machine => machine.BoquilhasId)
            .HasConstraintName(BoquilhasForeignKeyConstraintName)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(machine => new { machine.BoquilhasId, machine.Machine })
            .IsUnique()
            .HasDatabaseName(BoquilhasMachineUniqueConstraintName);
    }
}