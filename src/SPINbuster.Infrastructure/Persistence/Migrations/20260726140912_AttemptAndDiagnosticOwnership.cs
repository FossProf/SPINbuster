using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPINbuster.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AttemptAndDiagnosticOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_promotion_provenances_FragmentCandidateId",
                table: "promotion_provenances");

            migrationBuilder.DropIndex(
                name: "IX_promotion_diagnostics_FragmentCandidateId",
                table: "promotion_diagnostics");

            migrationBuilder.AddColumn<int>(
                name: "ConflictType",
                table: "promotion_diagnostics",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "promotion_attempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Outcome = table.Column<int>(type: "INTEGER", nullable: false),
                    DiagnosticId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FragmentCandidateId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContentHash = table.Column<string>(type: "TEXT", nullable: false),
                    AttemptedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    FailureReason = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_attempts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_promotion_provenances_FragmentCandidateId",
                table: "promotion_provenances",
                column: "FragmentCandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_diagnostics_FragmentCandidateId",
                table: "promotion_diagnostics",
                column: "FragmentCandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_attempts_DiagnosticId",
                table: "promotion_attempts",
                column: "DiagnosticId");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_attempts_FragmentCandidateId",
                table: "promotion_attempts",
                column: "FragmentCandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_attempts_RecordId",
                table: "promotion_attempts",
                column: "RecordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "promotion_attempts");

            migrationBuilder.DropIndex(
                name: "IX_promotion_provenances_FragmentCandidateId",
                table: "promotion_provenances");

            migrationBuilder.DropIndex(
                name: "IX_promotion_diagnostics_FragmentCandidateId",
                table: "promotion_diagnostics");

            migrationBuilder.DropColumn(
                name: "ConflictType",
                table: "promotion_diagnostics");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_provenances_FragmentCandidateId",
                table: "promotion_provenances",
                column: "FragmentCandidateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_promotion_diagnostics_FragmentCandidateId",
                table: "promotion_diagnostics",
                column: "FragmentCandidateId",
                unique: true);
        }
    }
}
