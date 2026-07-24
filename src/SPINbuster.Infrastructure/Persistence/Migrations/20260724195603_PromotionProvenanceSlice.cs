using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPINbuster.Infrastructure.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class PromotionProvenanceSlice : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<Guid>(
          name: "SourceImportedSourceId",
          table: "knowledge_relationships",
          type: "TEXT",
          nullable: true);

      migrationBuilder.AddColumn<Guid>(
          name: "TargetImportedSourceId",
          table: "knowledge_relationships",
          type: "TEXT",
          nullable: true);

      migrationBuilder.CreateTable(
          name: "promotion_provenances",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
            PromotedRevisionId = table.Column<Guid>(type: "TEXT", nullable: false),
            DiagnosticId = table.Column<Guid>(type: "TEXT", nullable: false),
            FragmentCandidateId = table.Column<Guid>(type: "TEXT", nullable: false),
            FragmentSourceContentHash = table.Column<string>(type: "TEXT", nullable: false),
            ReviewState = table.Column<int>(type: "INTEGER", nullable: false),
            ReviewedBy = table.Column<string>(type: "TEXT", nullable: true),
            ReviewedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
            ParserRunId = table.Column<Guid>(type: "TEXT", nullable: false),
            ParserKey = table.Column<string>(type: "TEXT", nullable: false),
            ParserVersion = table.Column<string>(type: "TEXT", nullable: false),
            ParserContractVersion = table.Column<string>(type: "TEXT", nullable: false),
            ParserContractHash = table.Column<string>(type: "TEXT", nullable: false),
            ImportedSourceId = table.Column<Guid>(type: "TEXT", nullable: false),
            ImportedSourceContentHash = table.Column<string>(type: "TEXT", nullable: false),
            PromotionIdentityHash = table.Column<string>(type: "TEXT", nullable: false),
            PromotionAttemptId = table.Column<Guid>(type: "TEXT", nullable: false),
            PromotedBy = table.Column<string>(type: "TEXT", nullable: false),
            PromotedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            PromotedAtUtcTicks = table.Column<long>(type: "INTEGER", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_promotion_provenances", x => x.Id);
            table.ForeignKey(
                      name: "FK_promotion_provenances_knowledge_document_revisions_PromotedRevisionId",
                      column: x => x.PromotedRevisionId,
                      principalTable: "knowledge_document_revisions",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
            table.ForeignKey(
                      name: "FK_promotion_provenances_parser_fragment_candidates_FragmentCandidateId",
                      column: x => x.FragmentCandidateId,
                      principalTable: "parser_fragment_candidates",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
            table.ForeignKey(
                      name: "FK_promotion_provenances_projects_ProjectId",
                      column: x => x.ProjectId,
                      principalTable: "projects",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
            table.ForeignKey(
                      name: "FK_promotion_provenances_promotion_diagnostics_DiagnosticId",
                      column: x => x.DiagnosticId,
                      principalTable: "promotion_diagnostics",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
          });

      migrationBuilder.CreateIndex(
          name: "IX_promotion_provenances_DiagnosticId",
          table: "promotion_provenances",
          column: "DiagnosticId",
          unique: true);

      migrationBuilder.CreateIndex(
          name: "IX_promotion_provenances_FragmentCandidateId",
          table: "promotion_provenances",
          column: "FragmentCandidateId",
          unique: true);

      migrationBuilder.CreateIndex(
          name: "IX_promotion_provenances_ProjectId",
          table: "promotion_provenances",
          column: "ProjectId");

      migrationBuilder.CreateIndex(
          name: "IX_promotion_provenances_PromotedRevisionId",
          table: "promotion_provenances",
          column: "PromotedRevisionId",
          unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "promotion_provenances");

      migrationBuilder.DropColumn(
          name: "SourceImportedSourceId",
          table: "knowledge_relationships");

      migrationBuilder.DropColumn(
          name: "TargetImportedSourceId",
          table: "knowledge_relationships");
    }
  }
}
