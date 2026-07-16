using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPINbuster.Infrastructure.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class ReportDraftSlice : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "Body",
          table: "reports");

      migrationBuilder.AddColumn<int>(
          name: "RevisionNumber",
          table: "reports",
          type: "INTEGER",
          nullable: false,
          defaultValue: 0);

      migrationBuilder.Sql("""
        UPDATE reports
        SET RevisionNumber = 1
        WHERE RevisionNumber = 0;
        """);

      migrationBuilder.CreateTable(
          name: "report_draft_operations",
          columns: table => new
          {
            OperationId = table.Column<Guid>(type: "TEXT", nullable: false),
            ReportId = table.Column<Guid>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_report_draft_operations", x => x.OperationId);
            table.ForeignKey(
                      name: "FK_report_draft_operations_reports_ReportId",
                      column: x => x.ReportId,
                      principalTable: "reports",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "report_evidence_sources",
          columns: table => new
          {
            ReportId = table.Column<Guid>(type: "TEXT", nullable: false),
            EvidenceAttachmentId = table.Column<Guid>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_report_evidence_sources", x => new { x.ReportId, x.EvidenceAttachmentId });
            table.ForeignKey(
                      name: "FK_report_evidence_sources_reports_ReportId",
                      column: x => x.ReportId,
                      principalTable: "reports",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "report_field_note_sources",
          columns: table => new
          {
            ReportId = table.Column<Guid>(type: "TEXT", nullable: false),
            FieldNoteId = table.Column<Guid>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_report_field_note_sources", x => new { x.ReportId, x.FieldNoteId });
            table.ForeignKey(
                      name: "FK_report_field_note_sources_reports_ReportId",
                      column: x => x.ReportId,
                      principalTable: "reports",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "report_sections",
          columns: table => new
          {
            ReportId = table.Column<Guid>(type: "TEXT", nullable: false),
            Position = table.Column<int>(type: "INTEGER", nullable: false),
            Heading = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            Content = table.Column<string>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_report_sections", x => new { x.ReportId, x.Position });
            table.ForeignKey(
                      name: "FK_report_sections_reports_ReportId",
                      column: x => x.ReportId,
                      principalTable: "reports",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateIndex(
          name: "IX_report_draft_operations_ReportId",
          table: "report_draft_operations",
          column: "ReportId",
          unique: true);

      migrationBuilder.CreateIndex(
          name: "IX_report_evidence_sources_EvidenceAttachmentId",
          table: "report_evidence_sources",
          column: "EvidenceAttachmentId");

      migrationBuilder.CreateIndex(
          name: "IX_report_field_note_sources_FieldNoteId",
          table: "report_field_note_sources",
          column: "FieldNoteId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "report_draft_operations");

      migrationBuilder.DropTable(
          name: "report_evidence_sources");

      migrationBuilder.DropTable(
          name: "report_field_note_sources");

      migrationBuilder.DropTable(
          name: "report_sections");

      migrationBuilder.DropColumn(
          name: "RevisionNumber",
          table: "reports");

      migrationBuilder.AddColumn<string>(
          name: "Body",
          table: "reports",
          type: "TEXT",
          nullable: false,
          defaultValue: "");
    }
  }
}
