using DMO.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMO.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// EF configuration for the <c>boquilha_close_snapshots</c> table: the immutable close snapshot.
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §6.5 (exact columns/CHECKs/FKs and the last-close index). Immutable
/// after COMMIT (no UPDATE/DELETE path); one row per close event; a reopen → close cycle writes a
/// NEW snapshot row. The snapshot never feeds any derivation (AC-C1/AC-B1).</remarks>
public sealed class BoquilhaCloseSnapshotEntityConfiguration : IEntityTypeConfiguration<BoquilhaCloseSnapshotEntity>
{
    /// <summary>Database name of the non-negative entrance-excess CHECK.</summary>
    public const string EntradaExcecionalCheckConstraintName = "boquilha_close_snapshots_entrada_excecional_check";

    /// <summary>Database name of the utilisation-range CHECK.</summary>
    public const string UtilisationCheckConstraintName = "boquilha_close_snapshots_utilisation_check";

    /// <summary>Database name of the owning-aggregate FK.</summary>
    public const string BoquilhasForeignKeyConstraintName = "FK_boquilha_close_snapshots_boquilhas_boquilhas_id";

    /// <summary>Database name of the closing-user FK.</summary>
    public const string ClosedByUserForeignKeyConstraintName = "FK_boquilha_close_snapshots_users_closed_by_user_id";

    /// <summary>Database name of the last-close-determination index.</summary>
    public const string BoquilhasCloseIndexName = "IX_boquilha_close_snapshots_boquilhas_id";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BoquilhaCloseSnapshotEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("boquilha_close_snapshots", table =>
        {
            table.HasCheckConstraint(
                EntradaExcecionalCheckConstraintName,
                "entrada_excecional >= 0");
            table.HasCheckConstraint(
                UtilisationCheckConstraintName,
                "utilisation_percent IS NULL OR (utilisation_percent >= 0 AND utilisation_percent <= 100)");
        });

        builder.HasKey(snapshot => snapshot.CloseSnapshotId);
        builder.Property(snapshot => snapshot.CloseSnapshotId)
            .HasColumnName("close_snapshot_id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(snapshot => snapshot.BoquilhasId)
            .HasColumnName("boquilhas_id")
            .IsRequired();

        builder.Property(snapshot => snapshot.ClosedByUserId)
            .HasColumnName("closed_by_user_id")
            .IsRequired();

        builder.Property(snapshot => snapshot.ClosedAt)
            .HasColumnName("closed_at")
            .IsRequired();

        builder.Property(snapshot => snapshot.InitialQuantity)
            .HasColumnName("initial_quantity")
            .IsRequired();

        builder.Property(snapshot => snapshot.OpeningDate)
            .HasColumnName("opening_date")
            .IsRequired();

        builder.Property(snapshot => snapshot.Disponivel)
            .HasColumnName("disponivel")
            .IsRequired();

        builder.Property(snapshot => snapshot.EmReparacao)
            .HasColumnName("em_reparacao")
            .IsRequired();

        builder.Property(snapshot => snapshot.Irreparavel)
            .HasColumnName("irreparavel")
            .IsRequired();

        builder.Property(snapshot => snapshot.EntradaExcecional)
            .HasColumnName("entrada_excecional")
            .IsRequired();

        builder.Property(snapshot => snapshot.UtilisationPercent)
            .HasColumnName("utilisation_percent")
            .HasPrecision(5, 2);

        builder.Property(snapshot => snapshot.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.HasOne<BoquilhaEntity>()
            .WithMany()
            .HasForeignKey(snapshot => snapshot.BoquilhasId)
            .HasConstraintName(BoquilhasForeignKeyConstraintName)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserEntity>()
            .WithMany()
            .HasForeignKey(snapshot => snapshot.ClosedByUserId)
            .HasConstraintName(ClosedByUserForeignKeyConstraintName)
            .OnDelete(DeleteBehavior.Restrict);

        // FK-supporting + "last close" determination (max closed_at, §23.3 step 7).
        builder.HasIndex(snapshot => new { snapshot.BoquilhasId, snapshot.ClosedAt })
            .HasDatabaseName(BoquilhasCloseIndexName);
    }
}