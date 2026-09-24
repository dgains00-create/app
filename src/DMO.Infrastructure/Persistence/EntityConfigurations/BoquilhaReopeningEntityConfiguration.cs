using DMO.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMO.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// EF configuration for the <c>boquilha_reopenings</c> table: the append-only reopen history.
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §6.6 (exact columns/CHECK/FKs and the reopen-history index).
/// Append-only after COMMIT (no UPDATE/DELETE path); reopen records are history, never a movement
/// type.</remarks>
public sealed class BoquilhaReopeningEntityConfiguration : IEntityTypeConfiguration<BoquilhaReopeningEntity>
{
    /// <summary>Database name of the required-reason CHECK.</summary>
    public const string ReasonRequiredCheckConstraintName = "boquilha_reopenings_reason_required_check";

    /// <summary>Database name of the owning-aggregate FK.</summary>
    public const string BoquilhasForeignKeyConstraintName = "FK_boquilha_reopenings_boquilhas_boquilhas_id";

    /// <summary>Database name of the exact-close FK.</summary>
    public const string CloseSnapshotForeignKeyConstraintName = "FK_boquilha_reopenings_boquilha_close_snapshots_close_snapshot_id";

    /// <summary>Database name of the reopening-user FK.</summary>
    public const string ReopenedByUserForeignKeyConstraintName = "FK_boquilha_reopenings_users_reopened_by_user_id";

    /// <summary>Database name of the reopen-history index.</summary>
    public const string BoquilhasReopenIndexName = "IX_boquilha_reopenings_boquilhas_id";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BoquilhaReopeningEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("boquilha_reopenings", table =>
        {
            table.HasCheckConstraint(
                ReasonRequiredCheckConstraintName,
                "btrim(reason) <> ''");
        });

        builder.HasKey(reopen => reopen.ReopenId);
        builder.Property(reopen => reopen.ReopenId)
            .HasColumnName("reopen_id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(reopen => reopen.BoquilhasId)
            .HasColumnName("boquilhas_id")
            .IsRequired();

        builder.Property(reopen => reopen.CloseSnapshotId)
            .HasColumnName("close_snapshot_id")
            .IsRequired();

        builder.Property(reopen => reopen.ReopenedByUserId)
            .HasColumnName("reopened_by_user_id")
            .IsRequired();

        builder.Property(reopen => reopen.ReopenedAt)
            .HasColumnName("reopened_at")
            .IsRequired();

        builder.Property(reopen => reopen.Reason)
            .HasColumnName("reason")
            .IsRequired();

        builder.Property(reopen => reopen.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.HasOne<BoquilhaEntity>()
            .WithMany()
            .HasForeignKey(reopen => reopen.BoquilhasId)
            .HasConstraintName(BoquilhasForeignKeyConstraintName)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<BoquilhaCloseSnapshotEntity>()
            .WithMany()
            .HasForeignKey(reopen => reopen.CloseSnapshotId)
            .HasConstraintName(CloseSnapshotForeignKeyConstraintName)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserEntity>()
            .WithMany()
            .HasForeignKey(reopen => reopen.ReopenedByUserId)
            .HasConstraintName(ReopenedByUserForeignKeyConstraintName)
            .OnDelete(DeleteBehavior.Restrict);

        // FK-supporting + the reopen-history read (§23.3/route 5 presence).
        builder.HasIndex(reopen => new { reopen.BoquilhasId, reopen.ReopenedAt })
            .HasDatabaseName(BoquilhasReopenIndexName);
    }
}