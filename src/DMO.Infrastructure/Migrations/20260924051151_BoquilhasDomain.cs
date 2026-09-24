using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DMO.Infrastructure.Migrations
{
    /// <summary>
    /// P2-T07 migration 007 — <c>BoquilhasDomain</c>, OWNER-CLARIFICATION correction: this pair
    /// REPLACES the unreviewed 20260924031924 pair (P2-T07 is not closed; migration 007 was never
    /// accepted by independent verification, so the lifecycle schema was corrected cleanly instead
    /// of preserving it with compensating legacy). The final schema is the production movement
    /// register: exactly THREE tables (<c>boquilhas</c> register identity with the plain
    /// <c>bq_id</c> UNIQUE key — one register per REAL production BQ context, no lifecycle state;
    /// <c>boquilha_movements</c> with the closed three-type CHECK
    /// <c>saida|entrada|entrada_sem_reparacao</c> and the Saída-required CHECK;
    /// <c>boquilha_movement_audit</c>). No <c>status</c>/<c>tool_id</c>/opening-fact columns, no
    /// <c>boquilha_machines</c>/<c>boquilha_close_snapshots</c>/<c>boquilha_reopenings</c> tables
    /// and NO ACTIVE partial unique index
    /// (<c>IX_boquilhas_active_bq_id</c>/<c>IX_boquilhas_active_tool_id</c> removed — the
    /// one-active-aggregate invariant no longer exists).
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
                    bq_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_boquilhas", x => x.boquilhas_id);
                    table.ForeignKey(
                        name: "FK_boquilhas_bq_contexts_bq_id",
                        column: x => x.bq_id,
                        principalTable: "bq_contexts",
                        principalColumn: "bq_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_boquilhas_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
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
                    observations = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_boquilha_movements", x => x.movement_id);
                    table.CheckConstraint("boquilha_movements_machine_check", "machine IS NULL OR machine IN ('B1','B2','B3','C1','C2','C3')");
                    table.CheckConstraint("boquilha_movements_observations_check", "observations IS NULL OR btrim(observations) <> ''");
                    table.CheckConstraint("boquilha_movements_quantity_check", "quantity > 0");
                    table.CheckConstraint("boquilha_movements_saida_required_check", "NOT (movement_type = 'saida' AND (machine IS NULL OR repairer_id IS NULL))");
                    table.CheckConstraint("boquilha_movements_type_check", "movement_type IN ('saida','entrada','entrada_sem_reparacao')");
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
                name: "boquilhas_bq_id_key",
                table: "boquilhas",
                column: "bq_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_boquilhas_created_at",
                table: "boquilhas",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_boquilhas_created_by_user_id",
                table: "boquilhas",
                column: "created_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "boquilha_movement_audit");

            migrationBuilder.DropTable(
                name: "boquilha_movements");

            migrationBuilder.DropTable(
                name: "boquilhas");
        }
    }
}
