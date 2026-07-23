using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPINbuster.Infrastructure.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class PromotionDiagnosticSlice : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "promotion_diagnostics",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            FragmentCandidateId = table.Column<Guid>(type: "TEXT", nullable: false),
            ParserRunId = table.Column<Guid>(type: "TEXT", nullable: false),
            ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
            PromotedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            Status = table.Column<int>(type: "INTEGER", nullable: false),
            FailureReason = table.Column<string>(type: "TEXT", nullable: true),
            KnowledgeDocumentId = table.Column<Guid>(type: "TEXT", nullable: true),
            KnowledgeDocumentRevisionId = table.Column<Guid>(type: "TEXT", nullable: true),
            KnowledgeCitationId = table.Column<Guid>(type: "TEXT", nullable: true),
            SupersededExistingRevision = table.Column<bool>(type: "INTEGER", nullable: false),
            SupersededRevisionId = table.Column<Guid>(type: "TEXT", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_promotion_diagnostics", x => x.Id);
            table.ForeignKey(
                      name: "FK_promotion_diagnostics_parser_fragment_candidates_FragmentCandidateId",
                      column: x => x.FragmentCandidateId,
                      principalTable: "parser_fragment_candidates",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
            table.ForeignKey(
                      name: "FK_promotion_diagnostics_parser_runs_ParserRunId",
                      column: x => x.ParserRunId,
                      principalTable: "parser_runs",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
            table.ForeignKey(
                      name: "FK_promotion_diagnostics_projects_ProjectId",
                      column: x => x.ProjectId,
                      principalTable: "projects",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
          });

      migrationBuilder.CreateIndex(
          name: "IX_promotion_diagnostics_FragmentCandidateId",
          table: "promotion_diagnostics",
          column: "FragmentCandidateId",
          unique: true);

      migrationBuilder.CreateIndex(
          name: "IX_promotion_diagnostics_ParserRunId",
          table: "promotion_diagnostics",
          column: "ParserRunId");

      migrationBuilder.CreateIndex(
          name: "IX_promotion_diagnostics_ProjectId",
          table: "promotion_diagnostics",
          column: "ProjectId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "promotion_diagnostics");
    }
  }
}
