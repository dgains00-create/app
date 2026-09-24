using DMO.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMO.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// EF configuration for the <c>boquilhas</c> table: the register identity row of one REAL
/// production/BQ context.
/// </summary>
/// <remarks>
/// Authority: P2-T07 OWNER CLARIFICATION. <c>bq_id</c> is NOT NULL with a plain UNIQUE key — one
/// register per production/BQ context, with NO lifecycle machinery (no <c>status</c>, no partial
/// unique active-anchor indexes). The FK chain <c>boquilhas.bq_id → bq_contexts → job_ons/tools</c>
/// carries the real production association.</remarks>
public sealed class BoquilhaEntityConfiguration : IEntityTypeConfiguration<BoquilhaEntity>
{
    /// <summary>Database name of the one-register-per-BQ-context unique key.</summary>
    public const string BqIdUniqueConstraintName = "boquilhas_bq_id_key";

    /// <summary>Database name of the production-anchor FK.</summary>
    public const string BqContextForeignKeyConstraintName = "FK_boquilhas_bq_contexts_bq_id";

    /// <summary>Database name of the opening-actor FK.</summary>
    public const string CreatedByUserForeignKeyConstraintName = "FK_boquilhas_users_created_by_user_id";

    /// <summary>Database name of the FK-supporting index on the register list traversal.</summary>
    public const string CreatedAtIndexName = "IX_boquilhas_created_at";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BoquilhaEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("boquilhas");

        builder.HasKey(register => register.BoquilhasId);
        builder.Property(register => register.BoquilhasId)
            .HasColumnName("boquilhas_id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(register => register.BqId)
            .HasColumnName("bq_id")
            .IsRequired();

        builder.Property(register => register.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(register => register.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.HasOne<BqContextEntity>()
            .WithMany()
            .HasForeignKey(register => register.BqId)
            .HasConstraintName(BqContextForeignKeyConstraintName)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserEntity>()
            .WithMany()
            .HasForeignKey(register => register.CreatedByUserId)
            .HasConstraintName(CreatedByUserForeignKeyConstraintName)
            .OnDelete(DeleteBehavior.Restrict);

        // One register per REAL production BQ context (the plain unique key; the lifecycle
        // ACTIVE partial unique indexes are superseded and removed).
        builder.HasIndex(register => register.BqId)
            .IsUnique()
            .HasDatabaseName(BqIdUniqueConstraintName);

        // The register list technical order (deterministic; creation order).
        builder.HasIndex(register => register.CreatedAt)
            .HasDatabaseName(CreatedAtIndexName);
    }
}