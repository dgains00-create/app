using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DMO.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ControloApproveDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "peso_review_decisions",
                columns: table => new
                {
                    peso_review_decision_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    peso_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision = table.Column<string>(type: "text", nullable: false),
                    decided_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    prior_status = table.Column<string>(type: "text", nullable: false),
                    pesos_version_at_decision = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_peso_review_decisions", x => x.peso_review_decision_id);
                    table.CheckConstraint("peso_review_decisions_decision_check", "decision IN ('aprovado','nao_aprovado','reaberto')");
                    table.CheckConstraint("peso_review_decisions_prior_status_check", "prior_status IN ('pendente','aprovado','nao_aprovado')");
                    table.CheckConstraint("peso_review_decisions_reason_required_check", "(decision = 'aprovado' AND reason IS NULL) OR (decision <> 'aprovado' AND btrim(reason) <> '')");
                    table.CheckConstraint("peso_review_decisions_version_check", "pesos_version_at_decision >= 1");
                    table.ForeignKey(
                        name: "FK_peso_review_decisions_pesos_peso_id",
                        column: x => x.peso_id,
                        principalTable: "pesos",
                        principalColumn: "peso_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_peso_review_decisions_users_decided_by_user_id",
                        column: x => x.decided_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pesos_reviewable",
                table: "pesos",
                columns: new[] { "status", "submitted_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_peso_review_decisions_decided_at",
                table: "peso_review_decisions",
                column: "decided_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_peso_review_decisions_decided_by_user_id",
                table: "peso_review_decisions",
                column: "decided_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_peso_review_decisions_peso_id",
                table: "peso_review_decisions",
                column: "peso_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "peso_review_decisions");

            migrationBuilder.DropIndex(
                name: "IX_pesos_reviewable",
                table: "pesos");
        }
    }
}
