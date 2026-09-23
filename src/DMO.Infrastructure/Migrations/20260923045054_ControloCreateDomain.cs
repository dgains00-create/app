using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DMO.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ControloCreateDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_lists",
                columns: table => new
                {
                    email_list_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_lists", x => x.email_list_id);
                    table.CheckConstraint("email_lists_name_required_check", "btrim(name) <> ''");
                });

            migrationBuilder.CreateTable(
                name: "email_templates",
                columns: table => new
                {
                    email_template_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "text", nullable: false),
                    subject = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    document_type = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_templates", x => x.email_template_id);
                    table.CheckConstraint("email_templates_body_required_check", "btrim(body) <> ''");
                    table.CheckConstraint("email_templates_document_type_check", "document_type IS NULL OR document_type IN ('peso','pegamentos','resumo')");
                    table.CheckConstraint("email_templates_name_required_check", "btrim(name) <> ''");
                    table.CheckConstraint("email_templates_subject_required_check", "btrim(subject) <> ''");
                });

            migrationBuilder.CreateTable(
                name: "pdf_directory_settings",
                columns: table => new
                {
                    pdf_directory_setting_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    singleton = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    base_directory = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pdf_directory_settings", x => x.pdf_directory_setting_id);
                    table.CheckConstraint("pdf_directory_settings_directory_required_check", "btrim(base_directory) <> ''");
                    table.CheckConstraint("pdf_directory_settings_singleton_check", "singleton");
                });

            migrationBuilder.CreateTable(
                name: "pesos",
                columns: table => new
                {
                    peso_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    cm_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tool_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "pendente"),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    submitted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    water_temperature = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false),
                    volume_marisa_bq = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    volume_puncao_pu = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    glass_density_g_cm3 = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    previous_production_end_reference = table.Column<string>(type: "text", nullable: true),
                    previous_average_weight_reference = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pesos", x => x.peso_id);
                    table.CheckConstraint("pesos_anchor_check", "((cm_id IS NULL)::int + (tool_id IS NULL)::int) = 1");
                    table.CheckConstraint("pesos_density_check", "glass_density_g_cm3 IS NULL OR glass_density_g_cm3 > 0");
                    table.CheckConstraint("pesos_marisa_volume_check", "volume_marisa_bq IS NULL OR volume_marisa_bq >= 0");
                    table.CheckConstraint("pesos_puncao_volume_check", "volume_puncao_pu IS NULL OR volume_puncao_pu >= 0");
                    table.CheckConstraint("pesos_sap_reference_check", "previous_production_end_reference IS NULL OR btrim(previous_production_end_reference) <> ''");
                    table.CheckConstraint("pesos_sap_weight_check", "previous_average_weight_reference IS NULL OR btrim(previous_average_weight_reference) <> ''");
                    table.CheckConstraint("pesos_status_check", "status IN ('pendente','aprovado','nao_aprovado')");
                    table.CheckConstraint("pesos_temperature_check", "water_temperature >= 5 AND water_temperature <= 35");
                    table.ForeignKey(
                        name: "FK_pesos_cm_contexts_cm_id",
                        column: x => x.cm_id,
                        principalTable: "cm_contexts",
                        principalColumn: "cm_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pesos_tools_tool_id",
                        column: x => x.tool_id,
                        principalTable: "tools",
                        principalColumn: "tool_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pesos_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pesos_users_submitted_by_user_id",
                        column: x => x.submitted_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "repairers",
                columns: table => new
                {
                    repairer_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_repairers", x => x.repairer_id);
                    table.CheckConstraint("repairers_name_required_check", "btrim(name) <> ''");
                });

            migrationBuilder.CreateTable(
                name: "email_list_recipients",
                columns: table => new
                {
                    email_list_recipient_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    email_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_list_recipients", x => x.email_list_recipient_id);
                    table.CheckConstraint("email_list_recipients_address_required_check", "btrim(address) <> ''");
                    table.ForeignKey(
                        name: "FK_email_list_recipients_email_lists_email_list_id",
                        column: x => x.email_list_id,
                        principalTable: "email_lists",
                        principalColumn: "email_list_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "peso_measurement_rows",
                columns: table => new
                {
                    peso_measurement_row_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    peso_id = table.Column<Guid>(type: "uuid", nullable: false),
                    row_position = table.Column<int>(type: "integer", nullable: false),
                    water_weight_g = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    capacity_cm3 = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    glass_weight_g = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_peso_measurement_rows", x => x.peso_measurement_row_id);
                    table.CheckConstraint("peso_measurement_rows_capacity_check", "capacity_cm3 > 0");
                    table.CheckConstraint("peso_measurement_rows_glass_check", "glass_weight_g > 0");
                    table.CheckConstraint("peso_measurement_rows_position_check", "row_position >= 1");
                    table.CheckConstraint("peso_measurement_rows_weight_check", "water_weight_g > 0");
                    table.ForeignKey(
                        name: "FK_peso_measurement_rows_pesos_peso_id",
                        column: x => x.peso_id,
                        principalTable: "pesos",
                        principalColumn: "peso_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "machine_repairer_assignments",
                columns: table => new
                {
                    machine_repairer_assignment_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    machine = table.Column<string>(type: "text", nullable: false),
                    repairer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_machine_repairer_assignments", x => x.machine_repairer_assignment_id);
                    table.CheckConstraint("machine_repairer_assignments_machine_check", "machine IN ('B1','B2','B3','C1','C2','C3')");
                    table.ForeignKey(
                        name: "FK_machine_repairer_assignments_repairers_repairer_id",
                        column: x => x.repairer_id,
                        principalTable: "repairers",
                        principalColumn: "repairer_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "email_list_recipients_list_address_key",
                table: "email_list_recipients",
                columns: new[] { "email_list_id", "address" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "email_lists_name_key",
                table: "email_lists",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "email_templates_name_key",
                table: "email_templates",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "machine_repairer_assignments_machine_key",
                table: "machine_repairer_assignments",
                column: "machine",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "machine_repairer_assignments_repairer_idx",
                table: "machine_repairer_assignments",
                column: "repairer_id");

            migrationBuilder.CreateIndex(
                name: "pdf_directory_settings_singleton_key",
                table: "pdf_directory_settings",
                column: "singleton",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "peso_measurement_rows_peso_position_key",
                table: "peso_measurement_rows",
                columns: new[] { "peso_id", "row_position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pesos_cm_id",
                table: "pesos",
                column: "cm_id");

            migrationBuilder.CreateIndex(
                name: "IX_pesos_created_by_user_id",
                table: "pesos",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_pesos_submitted_by_user_id",
                table: "pesos",
                column: "submitted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_pesos_tool_id",
                table: "pesos",
                column: "tool_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_list_recipients");

            migrationBuilder.DropTable(
                name: "email_templates");

            migrationBuilder.DropTable(
                name: "machine_repairer_assignments");

            migrationBuilder.DropTable(
                name: "pdf_directory_settings");

            migrationBuilder.DropTable(
                name: "peso_measurement_rows");

            migrationBuilder.DropTable(
                name: "email_lists");

            migrationBuilder.DropTable(
                name: "repairers");

            migrationBuilder.DropTable(
                name: "pesos");
        }
    }
}
