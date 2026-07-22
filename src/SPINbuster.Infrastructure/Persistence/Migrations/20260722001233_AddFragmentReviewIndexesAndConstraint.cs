using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861

namespace SPINbuster.Infrastructure.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class AddFragmentReviewIndexesAndConstraint : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateIndex(
          name: "IX_parser_fragment_candidates_ParserRunId_ReviewState",
          table: "parser_fragment_candidates",
          columns: new[] { "ParserRunId", "ReviewState" });

      migrationBuilder.CreateIndex(
          name: "IX_parser_fragment_candidates_ProjectId_ReviewState",
          table: "parser_fragment_candidates",
          columns: new[] { "ProjectId", "ReviewState" });

      migrationBuilder.AddCheckConstraint(
          name: "CK_parser_fragment_candidates_review_metadata",
          table: "parser_fragment_candidates",
          sql: "(ReviewState = 0 AND ReviewedBy IS NULL AND ReviewedAtUtc IS NULL) OR (ReviewState IN (1, 2) AND ReviewedBy IS NOT NULL AND ReviewedAtUtc IS NOT NULL)");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropIndex(
          name: "IX_parser_fragment_candidates_ParserRunId_ReviewState",
          table: "parser_fragment_candidates");

      migrationBuilder.DropIndex(
          name: "IX_parser_fragment_candidates_ProjectId_ReviewState",
          table: "parser_fragment_candidates");

      migrationBuilder.DropCheckConstraint(
          name: "CK_parser_fragment_candidates_review_metadata",
          table: "parser_fragment_candidates");
    }
  }
}

#pragma warning restore CA1861
