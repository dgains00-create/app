using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DMO.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GlassDensitySettings : Migration
    {
        /// <inheritdoc />
        /// <remarks>
        /// The ONLY delta of the post-closure glass-density correction (contract §5.1/§5.2): one
        /// additive settings table and its two initial operational rows. The table is a sibling of
        /// the five existing Definições settings tables; the seed is a deliberate, documented
        /// exception to the baseline's "no seeds" posture, superseded to this exact extent.
        /// </remarks>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "glass_density_settings",
                columns: table => new
                {
                    processo = table.Column<string>(type: "text", nullable: false),
                    density_g_cm3 = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_glass_density_settings", x => x.processo);
                    table.CheckConstraint("glass_density_settings_density_check", "density_g_cm3 > 0");
                    table.CheckConstraint("glass_density_settings_processo_check", "processo IN ('NNPB','PS')");
                });

            // Initial operational rows — the two provenance-backed authoritative values recovered
            // from the legacy production authority (correction contract §4: BA-DMO
            // PesoModuleCatalog the constants ConstantNnpb = 2.4027m / ConstantPs = 2.4231m,
            // peso_settings constant_nnpb/constant_ps defaults). NNPB and PS are the fixed
            // canonical process entries (Owner rule); these values are operator-editable from now
            // on and are NEVER re-created or overwritten by any later migration. The seed version
            // is 1 — the established sibling-settings convention (version DEFAULT 1): the first
            // guarded PUT from the freshly migrated surface carries expectedVersion 1 and writes
            // version 2, exactly like every other Definições settings row (Architect observation
            // N-2; the stale-version semantics are unchanged on any consistent token).
            migrationBuilder.InsertData(
                table: "glass_density_settings",
                columns: new[] { "processo", "density_g_cm3", "version" },
                values: new object[,]
                {
                    { "NNPB", 2.4027m, 1 },
                    { "PS", 2.4231m, 1 },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "glass_density_settings");
        }
    }
}
