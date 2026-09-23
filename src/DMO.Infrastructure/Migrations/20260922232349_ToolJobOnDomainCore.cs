using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DMO.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ToolJobOnDomainCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "job_ons",
                columns: table => new
                {
                    jobon_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    reference = table.Column<string>(type: "text", nullable: false),
                    production_number = table.Column<string>(type: "text", nullable: false),
                    machine = table.Column<string>(type: "text", nullable: false),
                    production_date = table.Column<DateOnly>(type: "date", nullable: true),
                    copied_from_jobon_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_ons", x => x.jobon_id);
                    table.CheckConstraint("job_ons_machine_check", "machine IN ('B1','B2','B3','C1','C2','C3')");
                    table.CheckConstraint("job_ons_production_number_required_check", "btrim(production_number) <> ''");
                    table.CheckConstraint("job_ons_reference_required_check", "btrim(reference) <> ''");
                    table.ForeignKey(
                        name: "FK_job_ons_job_ons_copied_from_jobon_id",
                        column: x => x.copied_from_jobon_id,
                        principalTable: "job_ons",
                        principalColumn: "jobon_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tools",
                columns: table => new
                {
                    tool_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tool_type = table.Column<string>(type: "text", nullable: false),
                    reference = table.Column<string>(type: "text", nullable: false),
                    lot = table.Column<string>(type: "text", nullable: false),
                    processo = table.Column<string>(type: "text", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tools", x => x.tool_id);
                    table.CheckConstraint("tools_lot_required_check", "btrim(lot) <> ''");
                    table.CheckConstraint("tools_processo_check", "processo IS NULL OR processo IN ('NNPB','PS')");
                    table.CheckConstraint("tools_quantity_check", "quantity IS NULL OR quantity >= 0");
                    table.CheckConstraint("tools_reference_required_check", "btrim(reference) <> ''");
                    table.CheckConstraint("tools_type_check", "tool_type IN ('CM','MF','BQ')");
                });

            migrationBuilder.CreateTable(
                name: "bq_contexts",
                columns: table => new
                {
                    bq_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    jobon_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tool_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tool_type = table.Column<string>(type: "text", nullable: false),
                    tool_reference = table.Column<string>(type: "text", nullable: false),
                    tool_lot = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bq_contexts", x => x.bq_id);
                    table.CheckConstraint("bq_contexts_tool_lot_required_check", "btrim(tool_lot) <> ''");
                    table.CheckConstraint("bq_contexts_tool_reference_required_check", "btrim(tool_reference) <> ''");
                    table.CheckConstraint("bq_contexts_tool_type_check", "tool_type = 'BQ'");
                    table.ForeignKey(
                        name: "FK_bq_contexts_job_ons_jobon_id",
                        column: x => x.jobon_id,
                        principalTable: "job_ons",
                        principalColumn: "jobon_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bq_contexts_tools_tool_id",
                        column: x => x.tool_id,
                        principalTable: "tools",
                        principalColumn: "tool_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cm_contexts",
                columns: table => new
                {
                    cm_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    jobon_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tool_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tool_type = table.Column<string>(type: "text", nullable: false),
                    tool_reference = table.Column<string>(type: "text", nullable: false),
                    tool_lot = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cm_contexts", x => x.cm_id);
                    table.CheckConstraint("cm_contexts_tool_lot_required_check", "btrim(tool_lot) <> ''");
                    table.CheckConstraint("cm_contexts_tool_reference_required_check", "btrim(tool_reference) <> ''");
                    table.CheckConstraint("cm_contexts_tool_type_check", "tool_type = 'CM'");
                    table.ForeignKey(
                        name: "FK_cm_contexts_job_ons_jobon_id",
                        column: x => x.jobon_id,
                        principalTable: "job_ons",
                        principalColumn: "jobon_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cm_contexts_tools_tool_id",
                        column: x => x.tool_id,
                        principalTable: "tools",
                        principalColumn: "tool_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mf_contexts",
                columns: table => new
                {
                    mf_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    jobon_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tool_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tool_type = table.Column<string>(type: "text", nullable: false),
                    tool_reference = table.Column<string>(type: "text", nullable: false),
                    tool_lot = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mf_contexts", x => x.mf_id);
                    table.CheckConstraint("mf_contexts_tool_lot_required_check", "btrim(tool_lot) <> ''");
                    table.CheckConstraint("mf_contexts_tool_reference_required_check", "btrim(tool_reference) <> ''");
                    table.CheckConstraint("mf_contexts_tool_type_check", "tool_type = 'MF'");
                    table.ForeignKey(
                        name: "FK_mf_contexts_job_ons_jobon_id",
                        column: x => x.jobon_id,
                        principalTable: "job_ons",
                        principalColumn: "jobon_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mf_contexts_tools_tool_id",
                        column: x => x.tool_id,
                        principalTable: "tools",
                        principalColumn: "tool_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tool_machines",
                columns: table => new
                {
                    tool_machine_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tool_id = table.Column<Guid>(type: "uuid", nullable: false),
                    machine = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tool_machines", x => x.tool_machine_id);
                    table.CheckConstraint("tool_machines_machine_check", "machine IN ('B1','B2','B3','C1','C2','C3')");
                    table.ForeignKey(
                        name: "FK_tool_machines_tools_tool_id",
                        column: x => x.tool_id,
                        principalTable: "tools",
                        principalColumn: "tool_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "bq_contexts_jobon_key",
                table: "bq_contexts",
                column: "jobon_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bq_contexts_tool_id",
                table: "bq_contexts",
                column: "tool_id");

            migrationBuilder.CreateIndex(
                name: "cm_contexts_jobon_key",
                table: "cm_contexts",
                column: "jobon_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cm_contexts_tool_id",
                table: "cm_contexts",
                column: "tool_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_ons_copied_from_jobon_id",
                table: "job_ons",
                column: "copied_from_jobon_id");

            migrationBuilder.CreateIndex(
                name: "job_ons_reference_production_number_key",
                table: "job_ons",
                columns: new[] { "reference", "production_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mf_contexts_tool_id",
                table: "mf_contexts",
                column: "tool_id");

            migrationBuilder.CreateIndex(
                name: "mf_contexts_jobon_key",
                table: "mf_contexts",
                column: "jobon_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "tool_machines_machine_idx",
                table: "tool_machines",
                column: "machine");

            migrationBuilder.CreateIndex(
                name: "tool_machines_tool_machine_key",
                table: "tool_machines",
                columns: new[] { "tool_id", "machine" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "tools_reference_idx",
                table: "tools",
                column: "reference");

            migrationBuilder.CreateIndex(
                name: "tools_type_reference_lot_key",
                table: "tools",
                columns: new[] { "tool_type", "reference", "lot" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bq_contexts");

            migrationBuilder.DropTable(
                name: "cm_contexts");

            migrationBuilder.DropTable(
                name: "mf_contexts");

            migrationBuilder.DropTable(
                name: "tool_machines");

            migrationBuilder.DropTable(
                name: "job_ons");

            migrationBuilder.DropTable(
                name: "tools");
        }
    }
}
