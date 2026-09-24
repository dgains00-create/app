using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DMO.Infrastructure.Migrations
{
    /// <summary>
    /// P2-T07 migration 007 — <c>BoquilhasDomain</c>: exactly the six contracted Boquilhas tables
    /// (contract §28). EF-generated with one documented post-edit: EF's model consolidation folds
    /// the contracted single-column FK-supporting/traversal indexes
    /// <c>IX_boquilhas_bq_id</c> / <c>IX_boquilhas_tool_id</c> into the partial unique indexes on
    /// the same columns, while §7.5 requires both; the migration therefore re-declares the two
    /// plain indexes explicitly (physical facts owned by this migration; the EF model snapshot
    /// deliberately carries only the unified model — a documented seam for any future migration).
    /// </summary>
    public partial class BoquilhasDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "boquilhas",
                columns: table => new
                {
                    boquilhas_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    bq_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tool_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    opening_date = table.Column<DateOnly>(type: "date", nullable: false),
                    utilisation_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    observations = table.Column<string>(type: "text", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_boquilhas", x => x.boquilhas_id);
                    table.CheckConstraint("boquilhas_anchor_exclusive_check", "((bq_id IS NULL)::int + (tool_id IS NULL)::int) = 1");
                    table.CheckConstraint("boquilhas_observations_check", "observations IS NULL OR btrim(observations) <> ''");
                    table.CheckConstraint("boquilhas_status_check", "status IN ('active','closed')");
                    table.CheckConstraint("boquilhas_utilisation_range_check", "utilisation_percent IS NULL OR (utilisation_percent >= 0 AND utilisation_percent <= 100)");
                    table.ForeignKey(
                        name: "FK_boquilhas_bq_contexts_bq_id",
                        column: x => x.bq_id,
                        principalTable: "bq_contexts",
                        principalColumn: "bq_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_boquilhas_tools_tool_id",
                        column: x => x.tool_id,
                        principalTable: "tools",
                        principalColumn: "tool_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_boquilhas_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "boquilha_close_snapshots",
                columns: table => new
                {
                    close_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    boquilhas_id = table.Column<Guid>(type: "uuid", nullable: false),
                    closed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    initial_quantity = table.Column<int>(type: "integer", nullable: false),
                    opening_date = table.Column<DateOnly>(type: "date", nullable: false),
                    disponivel = table.Column<int>(type: "integer", nullable: false),
                    em_reparacao = table.Column<int>(type: "integer", nullable: false),
                    irreparavel = table.Column<int>(type: "integer", nullable: false),
                    entrada_excecional = table.Column<int>(type: "integer", nullable: false),
                    utilisation_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_boquilha_close_snapshots", x => x.close_snapshot_id);
                    table.CheckConstraint("boquilha_close_snapshots_entrada_excecional_check", "entrada_excecional >= 0");
                    table.CheckConstraint("boquilha_close_snapshots_utilisation_check", "utilisation_percent IS NULL OR (utilisation_percent >= 0 AND utilisation_percent <= 100)");
                    table.ForeignKey(
                        name: "FK_boquilha_close_snapshots_boquilhas_boquilhas_id",
                        column: x => x.boquilhas_id,
                        principalTable: "boquilhas",
                        principalColumn: "boquilhas_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_boquilha_close_snapshots_users_closed_by_user_id",
                        column: x => x.closed_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "boquilha_machines",
                columns: table => new
                {
                    boquilha_machine_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    boquilhas_id = table.Column<Guid>(type: "uuid", nullable: false),
                    machine = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_boquilha_machines", x => x.boquilha_machine_id);
                    table.CheckConstraint("boquilha_machines_machine_check", "machine IN ('B1','B2','B3','C1','C2','C3')");
                    table.ForeignKey(
                        name: "FK_boquilha_machines_boquilhas_boquilhas_id",
                        column: x => x.boquilhas_id,
                        principalTable: "boquilhas",
                        principalColumn: "boquilhas_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "boquilha_movements",
                columns: table => new
                {
                    movement_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    boquilhas_id = table.Column<Guid>(type: "uuid", nullable: false),
                    movement_type = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recorded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    machine = table.Column<string>(type: "text", nullable: true),
                    repairer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    expected_return_quantity = table.Column<int>(type: "integer", nullable: true),
                    excess_received_quantity = table.Column<int>(type: "integer", nullable: true),
                    observations = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_boquilha_movements", x => x.movement_id);
                    table.CheckConstraint("boquilha_movements_entrada_facts_check", "(movement_type = 'entrada' AND expected_return_quantity IS NOT NULL AND excess_received_quantity IS NOT NULL AND expected_return_quantity >= 0 AND excess_received_quantity = GREATEST(0, quantity - expected_return_quantity)) OR (movement_type <> 'entrada' AND expected_return_quantity IS NULL AND excess_received_quantity IS NULL)");
                    table.CheckConstraint("boquilha_movements_machine_check", "machine IS NULL OR machine IN ('B1','B2','B3','C1','C2','C3')");
                    table.CheckConstraint("boquilha_movements_observations_check", "observations IS NULL OR btrim(observations) <> ''");
                    table.CheckConstraint("boquilha_movements_quantity_check", "quantity > 0");
                    table.CheckConstraint("boquilha_movements_saida_required_check", "NOT (movement_type = 'saida' AND (machine IS NULL OR repairer_id IS NULL))");
                    table.CheckConstraint("boquilha_movements_type_check", "movement_type IN ('inicio','saida','entrada','irreparavel')");
                    table.CheckConstraint("boquilha_movements_version_check", "version >= 1");
                    table.ForeignKey(
                        name: "FK_boquilha_movements_boquilhas_boquilhas_id",
                        column: x => x.boquilhas_id,
                        principalTable: "boquilhas",
                        principalColumn: "boquilhas_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_boquilha_movements_repairers_repairer_id",
                        column: x => x.repairer_id,
                        principalTable: "repairers",
                        principalColumn: "repairer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_boquilha_movements_users_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "boquilha_reopenings",
                columns: table => new
                {
                    reopen_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    boquilhas_id = table.Column<Guid>(type: "uuid", nullable: false),
                    close_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reopened_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reopened_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_boquilha_reopenings", x => x.reopen_id);
                    table.CheckConstraint("boquilha_reopenings_reason_required_check", "btrim(reason) <> ''");
                    table.ForeignKey(
                        name: "FK_boquilha_reopenings_boquilha_close_snapshots_close_snapshot_id",
                        column: x => x.close_snapshot_id,
                        principalTable: "boquilha_close_snapshots",
                        principalColumn: "close_snapshot_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_boquilha_reopenings_boquilhas_boquilhas_id",
                        column: x => x.boquilhas_id,
                        principalTable: "boquilhas",
                        principalColumn: "boquilhas_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_boquilha_reopenings_users_reopened_by_user_id",
                        column: x => x.reopened_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "boquilha_movement_audit",
                columns: table => new
                {
                    movement_audit_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    movement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    edited_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    edited_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    before_quantity = table.Column<int>(type: "integer", nullable: false),
                    after_quantity = table.Column<int>(type: "integer", nullable: false),
                    before_business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    after_business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    before_machine = table.Column<string>(type: "text", nullable: true),
                    after_machine = table.Column<string>(type: "text", nullable: true),
                    before_repairer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    after_repairer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    before_observations = table.Column<string>(type: "text", nullable: true),
                    after_observations = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_boquilha_movement_audit", x => x.movement_audit_id);
                    table.CheckConstraint("boquilha_movement_audit_quantities_check", "before_quantity > 0 AND after_quantity > 0");
                    table.ForeignKey(
                        name: "FK_boquilha_movement_audit_boquilha_movements_movement_id",
                        column: x => x.movement_id,
                        principalTable: "boquilha_movements",
                        principalColumn: "movement_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_boquilha_movement_audit_users_edited_by_user_id",
                        column: x => x.edited_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_boquilha_close_snapshots_boquilhas_id",
                table: "boquilha_close_snapshots",
                columns: new[] { "boquilhas_id", "closed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_boquilha_close_snapshots_closed_by_user_id",
                table: "boquilha_close_snapshots",
                column: "closed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "boquilha_machines_boquilhas_machine_key",
                table: "boquilha_machines",
                columns: new[] { "boquilhas_id", "machine" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_boquilha_movement_audit_edited_by_user_id",
                table: "boquilha_movement_audit",
                column: "edited_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_boquilha_movement_audit_movement_id",
                table: "boquilha_movement_audit",
                columns: new[] { "movement_id", "edited_at" });

            migrationBuilder.CreateIndex(
                name: "IX_boquilha_movements_boquilhas_id",
                table: "boquilha_movements",
                columns: new[] { "boquilhas_id", "recorded_at", "movement_id" });

            migrationBuilder.CreateIndex(
                name: "IX_boquilha_movements_recorded_by_user_id",
                table: "boquilha_movements",
                column: "recorded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_boquilha_movements_repairer_id",
                table: "boquilha_movements",
                column: "repairer_id");

            migrationBuilder.CreateIndex(
                name: "IX_boquilha_reopenings_boquilhas_id",
                table: "boquilha_reopenings",
                columns: new[] { "boquilhas_id", "reopened_at" });

            migrationBuilder.CreateIndex(
                name: "IX_boquilha_reopenings_close_snapshot_id",
                table: "boquilha_reopenings",
                column: "close_snapshot_id");

            migrationBuilder.CreateIndex(
                name: "IX_boquilha_reopenings_reopened_by_user_id",
                table: "boquilha_reopenings",
                column: "reopened_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_boquilhas_active_bq_id",
                table: "boquilhas",
                column: "bq_id",
                unique: true,
                filter: "\"status\" = 'active' AND \"bq_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_boquilhas_active_tool_id",
                table: "boquilhas",
                column: "tool_id",
                unique: true,
                filter: "\"status\" = 'active' AND \"tool_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_boquilhas_bq_id",
                table: "boquilhas",
                column: "bq_id");

            migrationBuilder.CreateIndex(
                name: "IX_boquilhas_tool_id",
                table: "boquilhas",
                column: "tool_id");

            migrationBuilder.CreateIndex(
                name: "IX_boquilhas_created_by_user_id",
                table: "boquilhas",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_boquilhas_status",
                table: "boquilhas",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "boquilha_machines");

            migrationBuilder.DropTable(
                name: "boquilha_movement_audit");

            migrationBuilder.DropTable(
                name: "boquilha_reopenings");

            migrationBuilder.DropTable(
                name: "boquilha_movements");

            migrationBuilder.DropTable(
                name: "boquilha_close_snapshots");

            migrationBuilder.DropTable(
                name: "boquilhas");
        }
    }
}
