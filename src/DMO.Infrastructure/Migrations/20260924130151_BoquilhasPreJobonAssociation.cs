using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DMO.Infrastructure.Migrations
{
    /// <summary>
    /// Delta migration 008 — <c>BoquilhasPreJobonAssociation</c>, OWNER-CLARIFICATION §34 delta:
    /// the <c>boquilhas</c> register gains the TRANSITIONAL pré-JobOn anchor. <c>bq_id</c> becomes
    /// nullable; the new <c>tool_id</c> column (FK → <c>tools</c> RESTRICT) holds the provisional
    /// canonical BQ Tool anchor while no Job On exists; the CHECK <c>boquilhas_anchor_check</c>
    /// enforces EXACTLY ONE anchor (<c>(bq_id IS NULL) &lt;&gt; (tool_id IS NULL)</c>) — never both,
    /// never neither, so <c>tool_id</c> and <c>bq_id</c> can never be two concurrent operational
    /// authorities; the new <c>version</c> column (default 1, CHECK ≥ 1, EF concurrency token) is
    /// the optimistic token of the sole register-row write (the association). Existing rows keep
    /// <c>bq_id</c> set and <c>tool_id</c> NULL, satisfying the anchor CHECK. No lifecycle column,
    /// no partial unique index, no new table; the one-register-per-<c>bq_id</c> unique key is
    /// unchanged (NULLs never collide). The permanent standalone model stays removed — the
    /// <c>tool_id</c> anchor is transitional by contract (§34.1) and is cleared on the
    /// human-confirmed association.
    /// </summary>
    public partial class BoquilhasPreJobonAssociation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "bq_id",
                table: "boquilhas",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "tool_id",
                table: "boquilhas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "boquilhas",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_boquilhas_tool_id",
                table: "boquilhas",
                column: "tool_id");

            migrationBuilder.AddCheckConstraint(
                name: "boquilhas_anchor_check",
                table: "boquilhas",
                sql: "(bq_id IS NULL) <> (tool_id IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "boquilhas_version_check",
                table: "boquilhas",
                sql: "version >= 1");

            migrationBuilder.AddForeignKey(
                name: "FK_boquilhas_tools_tool_id",
                table: "boquilhas",
                column: "tool_id",
                principalTable: "tools",
                principalColumn: "tool_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_boquilhas_tools_tool_id",
                table: "boquilhas");

            migrationBuilder.DropIndex(
                name: "IX_boquilhas_tool_id",
                table: "boquilhas");

            migrationBuilder.DropCheckConstraint(
                name: "boquilhas_anchor_check",
                table: "boquilhas");

            migrationBuilder.DropCheckConstraint(
                name: "boquilhas_version_check",
                table: "boquilhas");

            migrationBuilder.DropColumn(
                name: "tool_id",
                table: "boquilhas");

            migrationBuilder.DropColumn(
                name: "version",
                table: "boquilhas");

            migrationBuilder.AlterColumn<Guid>(
                name: "bq_id",
                table: "boquilhas",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
