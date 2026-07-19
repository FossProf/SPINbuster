using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861

namespace SPINbuster.Infrastructure.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class ParsingFoundationSlice : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "parser_runs",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
            ImportedSourceId = table.Column<Guid>(type: "TEXT", nullable: false),
            ParserKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            ParserVersion = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
            ParserContractVersion = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
            ParserContractHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            SourceContentHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            SourceHashAlgorithm = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
            SourceHashAlgorithmVersion = table.Column<int>(type: "INTEGER", nullable: false),
            CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            State = table.Column<int>(type: "INTEGER", nullable: false),
            StartedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
            CompletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
            FailureReason = table.Column<string>(type: "TEXT", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_parser_runs", x => x.Id);
            table.ForeignKey(
                      name: "FK_parser_runs_imported_document_sources_ImportedSourceId",
                      column: x => x.ImportedSourceId,
                      principalTable: "imported_document_sources",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
            table.ForeignKey(
                      name: "FK_parser_runs_projects_ProjectId",
                      column: x => x.ProjectId,
                      principalTable: "projects",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
          });

      migrationBuilder.CreateTable(
          name: "parser_fragment_candidates",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            ParserRunId = table.Column<Guid>(type: "TEXT", nullable: false),
            ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
            ImportedSourceId = table.Column<Guid>(type: "TEXT", nullable: false),
            SourceContentHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            LocatorType = table.Column<int>(type: "INTEGER", nullable: false),
            LocatorRawValue = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
            LocatorNormalizedValue = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
            Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
            ContentKind = table.Column<int>(type: "INTEGER", nullable: false),
            ExtractedText = table.Column<string>(type: "TEXT", nullable: false),
            TextLength = table.Column<int>(type: "INTEGER", nullable: false),
            ConfidenceBand = table.Column<int>(type: "INTEGER", nullable: false),
            IdentityKey = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
            IdentityKeyHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_parser_fragment_candidates", x => x.Id);
            table.ForeignKey(
                      name: "FK_parser_fragment_candidates_imported_document_sources_ImportedSourceId",
                      column: x => x.ImportedSourceId,
                      principalTable: "imported_document_sources",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
            table.ForeignKey(
                      name: "FK_parser_fragment_candidates_parser_runs_ParserRunId",
                      column: x => x.ParserRunId,
                      principalTable: "parser_runs",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
          });

      migrationBuilder.CreateIndex(
          name: "IX_parser_fragment_candidates_IdentityKeyHash",
          table: "parser_fragment_candidates",
          column: "IdentityKeyHash");

      migrationBuilder.CreateIndex(
          name: "IX_parser_fragment_candidates_ImportedSourceId",
          table: "parser_fragment_candidates",
          column: "ImportedSourceId");

      migrationBuilder.CreateIndex(
          name: "IX_parser_fragment_candidates_ParserRunId",
          table: "parser_fragment_candidates",
          column: "ParserRunId");

      migrationBuilder.CreateIndex(
          name: "IX_parser_runs_ImportedSourceId",
          table: "parser_runs",
          column: "ImportedSourceId");

      migrationBuilder.CreateIndex(
          name: "IX_parser_runs_ImportedSourceId_ParserKey_ParserVersion_ParserContractVersion_ParserContractHash",
          table: "parser_runs",
          columns: new[] { "ImportedSourceId", "ParserKey", "ParserVersion", "ParserContractVersion", "ParserContractHash" },
          unique: true);

      migrationBuilder.CreateIndex(
          name: "IX_parser_runs_ProjectId",
          table: "parser_runs",
          column: "ProjectId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "parser_fragment_candidates");

      migrationBuilder.DropTable(
          name: "parser_runs");
    }
  }
}

#pragma warning restore CA1861
