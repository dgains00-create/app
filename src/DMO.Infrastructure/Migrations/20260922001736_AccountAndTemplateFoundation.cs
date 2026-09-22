using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DMO.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AccountAndTemplateFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_accounts",
                columns: table => new
                {
                    admin_id = table.Column<Guid>(type: "uuid", nullable: false),
                    auth_identity_id = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_accounts", x => x.admin_id);
                    table.CheckConstraint("admin_accounts_auth_identity_id_required_check", "btrim(auth_identity_id) <> ''");
                    table.CheckConstraint("admin_accounts_email_required_check", "btrim(email) <> ''");
                    table.CheckConstraint("admin_accounts_singleton_id_check", "admin_id = '00000000-0000-4000-8000-0000000000ad'::uuid");
                });

            migrationBuilder.CreateTable(
                name: "templates",
                columns: table => new
                {
                    template_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "text", nullable: false),
                    landing_destination_id = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_templates", x => x.template_id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    auth_identity_id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    company_number = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.user_id);
                    table.CheckConstraint("users_auth_identity_id_required_check", "btrim(auth_identity_id) <> ''");
                    table.CheckConstraint("users_company_number_required_check", "btrim(company_number) <> ''");
                    table.CheckConstraint("users_email_required_check", "btrim(email) <> ''");
                    table.ForeignKey(
                        name: "FK_users_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "templates",
                        principalColumn: "template_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "admin_accounts_active_idx",
                table: "admin_accounts",
                column: "active",
                filter: "\"active\"");

            migrationBuilder.CreateIndex(
                name: "admin_accounts_auth_identity_id_key",
                table: "admin_accounts",
                column: "auth_identity_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "admin_accounts_email_key",
                table: "admin_accounts",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_template_id",
                table: "users",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "users_active_idx",
                table: "users",
                column: "active",
                filter: "\"active\"");

            migrationBuilder.CreateIndex(
                name: "users_auth_identity_id_key",
                table: "users",
                column: "auth_identity_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "users_company_number_key",
                table: "users",
                column: "company_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_accounts");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "templates");
        }
    }
}
