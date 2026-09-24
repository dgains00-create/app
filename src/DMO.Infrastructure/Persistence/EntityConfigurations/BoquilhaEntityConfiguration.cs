using DMO.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMO.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// EF configuration for the <c>boquilhas</c> table: one collective BQ external-repair aggregate.
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §6.1/§7 (exact columns, CHECKs, FKs all RESTRICT, indexes incl. the
/// two ACTIVE partial unique indexes of the B1 correction). The exclusive-anchor CHECK guarantees a
/// row satisfies at most one of the two partial predicates; the partial unique indexes are the
/// race-safe one-active-per-anchor backstop (create and reopen losers raise 23505 →
/// <c>Refused(ActiveAggregateExists)</c>, §7.2).
/// </remarks>
public sealed class BoquilhaEntityConfiguration : IEntityTypeConfiguration<BoquilhaEntity>
{
    /// <summary>Database name of the exclusive-anchor CHECK.</summary>
    public const string AnchorExclusiveCheckConstraintName = "boquilhas_anchor_exclusive_check";

    /// <summary>Database name of the status CHECK.</summary>
    public const string StatusCheckConstraintName = "boquilhas_status_check";

    /// <summary>Database name of the utilisation-range CHECK.</summary>
    public const string UtilisationRangeCheckConstraintName = "boquilhas_utilisation_range_check";

    /// <summary>Database name of the observations CHECK.</summary>
    public const string ObservationsCheckConstraintName = "boquilhas_observations_check";

    /// <summary>Database name of the production-linked anchor FK.</summary>
    public const string BqContextForeignKeyConstraintName = "FK_boquilhas_bq_contexts_bq_id";

    /// <summary>Database name of the standalone anchor FK.</summary>
    public const string ToolForeignKeyConstraintName = "FK_boquilhas_tools_tool_id";

    /// <summary>Database name of the opening-actor FK.</summary>
    public const string CreatedByUserForeignKeyConstraintName = "FK_boquilhas_users_created_by_user_id";

    /// <summary>Database name of the status index (Registo grid + Histórico state filter).</summary>
    public const string StatusIndexName = "IX_boquilhas_status";

    /// <summary>Database name of the FK-supporting production-linked anchor index.</summary>
    public const string BqIdIndexName = "IX_boquilhas_bq_id";

    /// <summary>Database name of the FK-supporting standalone anchor index.</summary>
    public const string ToolIdIndexName = "IX_boquilhas_tool_id";

    /// <summary>
    /// Database name of the race-safe one-active-per-production-linked-anchor partial unique index
    /// (WHERE status = 'active' AND bq_id IS NOT NULL; §7.2/§7.5).
    /// </summary>
    public const string ActiveBqIdUniqueIndexName = "IX_boquilhas_active_bq_id";

    /// <summary>
    /// Database name of the race-safe one-active-per-standalone-anchor partial unique index
    /// (WHERE status = 'active' AND tool_id IS NOT NULL; §7.2/§7.5).
    /// </summary>
    public const string ActiveToolIdUniqueIndexName = "IX_boquilhas_active_tool_id";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BoquilhaEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("boquilhas", table =>
        {
            table.HasCheckConstraint(
                AnchorExclusiveCheckConstraintName,
                "((bq_id IS NULL)::int + (tool_id IS NULL)::int) = 1");
            table.HasCheckConstraint(
                StatusCheckConstraintName,
                "status IN ('active','closed')");
            table.HasCheckConstraint(
                UtilisationRangeCheckConstraintName,
                "utilisation_percent IS NULL OR (utilisation_percent >= 0 AND utilisation_percent <= 100)");
            table.HasCheckConstraint(
                ObservationsCheckConstraintName,
                "observations IS NULL OR btrim(observations) <> ''");
        });

        builder.HasKey(aggregate => aggregate.BoquilhasId);
        builder.Property(aggregate => aggregate.BoquilhasId)
            .HasColumnName("boquilhas_id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(aggregate => aggregate.BqId)
            .HasColumnName("bq_id");

        builder.Property(aggregate => aggregate.ToolId)
            .HasColumnName("tool_id");

        builder.Property(aggregate => aggregate.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(aggregate => aggregate.OpeningDate)
            .HasColumnName("opening_date")
            .IsRequired();

        builder.Property(aggregate => aggregate.UtilisationPercent)
            .HasColumnName("utilisation_percent")
            .HasPrecision(5, 2);

        builder.Property(aggregate => aggregate.Observations)
            .HasColumnName("observations");

        builder.Property(aggregate => aggregate.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(aggregate => aggregate.Version)
            .HasColumnName("version")
            .HasDefaultValue(1)
            .IsConcurrencyToken();

        builder.Property(aggregate => aggregate.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(aggregate => aggregate.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        builder.HasOne<BqContextEntity>()
            .WithMany()
            .HasForeignKey(aggregate => aggregate.BqId)
            .HasConstraintName(BqContextForeignKeyConstraintName)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ToolEntity>()
            .WithMany()
            .HasForeignKey(aggregate => aggregate.ToolId)
            .HasConstraintName(ToolForeignKeyConstraintName)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserEntity>()
            .WithMany()
            .HasForeignKey(aggregate => aggregate.CreatedByUserId)
            .HasConstraintName(CreatedByUserForeignKeyConstraintName)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(aggregate => aggregate.Status)
            .HasDatabaseName(StatusIndexName);

        builder.HasIndex(aggregate => aggregate.BqId)
            .HasDatabaseName(BqIdIndexName);

        builder.HasIndex(aggregate => aggregate.ToolId)
            .HasDatabaseName(ToolIdIndexName);

        // The B1-correction race-safe backstops: UNIQUE PARTIAL against ACTIVE rows only — closed/
        // historical rows are unconstrained (multiple traces over time are legitimate, §7.2).
        builder.HasIndex(aggregate => aggregate.BqId)
            .IsUnique()
            .HasFilter("\"status\" = 'active' AND \"bq_id\" IS NOT NULL")
            .HasDatabaseName(ActiveBqIdUniqueIndexName);

        builder.HasIndex(aggregate => aggregate.ToolId)
            .IsUnique()
            .HasFilter("\"status\" = 'active' AND \"tool_id\" IS NOT NULL")
            .HasDatabaseName(ActiveToolIdUniqueIndexName);
    }
}