using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPINbuster.Infrastructure.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class AddParserDiagnostics : Migration
  {
    private static readonly string[] DiagnosticIndexColumns = ["ParserRunId", "CandidateRefValue"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "parser_diagnostics",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            ParserRunId = table.Column<Guid>(type: "TEXT", nullable: false),
            Severity = table.Column<int>(type: "INTEGER", nullable: false),
            Code = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
            Message = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            CandidateRefType = table.Column<int>(type: "INTEGER", nullable: true),
            CandidateRefValue = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
            LocatorType = table.Column<int>(type: "INTEGER", nullable: true),
            LocatorValue = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_parser_diagnostics", x => x.Id);
            table.ForeignKey(
                      name: "FK_parser_diagnostics_parser_runs_ParserRunId",
                      column: x => x.ParserRunId,
                      principalTable: "parser_runs",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
          });

      migrationBuilder.CreateIndex(
          name: "IX_parser_diagnostics_ParserRunId",
          table: "parser_diagnostics",
          column: "ParserRunId");

      migrationBuilder.CreateIndex(
          name: "IX_parser_diagnostics_ParserRunId_CandidateRefValue",
          table: "parser_diagnostics",
          columns: DiagnosticIndexColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "parser_diagnostics");
    }
  }
}
