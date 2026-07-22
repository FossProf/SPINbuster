using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPINbuster.Infrastructure.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class AddFragmentCandidateReviewState : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<string>(
          name: "ReviewNotes",
          table: "parser_fragment_candidates",
          type: "TEXT",
          maxLength: 2000,
          nullable: true);

      migrationBuilder.AddColumn<int>(
          name: "ReviewState",
          table: "parser_fragment_candidates",
          type: "INTEGER",
          nullable: false,
          defaultValue: 0);

      migrationBuilder.AddColumn<DateTimeOffset>(
          name: "ReviewedAtUtc",
          table: "parser_fragment_candidates",
          type: "TEXT",
          nullable: true);

      migrationBuilder.AddColumn<string>(
          name: "ReviewedBy",
          table: "parser_fragment_candidates",
          type: "TEXT",
          maxLength: 256,
          nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "ReviewNotes",
          table: "parser_fragment_candidates");

      migrationBuilder.DropColumn(
          name: "ReviewState",
          table: "parser_fragment_candidates");

      migrationBuilder.DropColumn(
          name: "ReviewedAtUtc",
          table: "parser_fragment_candidates");

      migrationBuilder.DropColumn(
          name: "ReviewedBy",
          table: "parser_fragment_candidates");
    }
  }
}
