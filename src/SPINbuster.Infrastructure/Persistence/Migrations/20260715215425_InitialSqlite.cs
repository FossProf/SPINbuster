using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPINbuster.Infrastructure.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class InitialSqlite : Migration
  {
    private static readonly string[] AuditEventIndexColumns = ["SubjectType", "SubjectId", "OccurredAtUtc"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "audit_events",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            SubjectType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            SubjectId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
            EventType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            Actor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            OccurredAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            Description = table.Column<string>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_audit_events", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "projects",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            Lifecycle = table.Column<int>(type: "INTEGER", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_projects", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "inspection_sessions",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
            Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            Lifecycle = table.Column<int>(type: "INTEGER", nullable: false),
            StartedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
            CompletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_inspection_sessions", x => x.Id);
            table.ForeignKey(
                      name: "FK_inspection_sessions_projects_ProjectId",
                      column: x => x.ProjectId,
                      principalTable: "projects",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
          });

      migrationBuilder.CreateTable(
          name: "evidence_attachments",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            InspectionSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
            CapturedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            CapturedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            FileName = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
            MediaType = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            StorageKey = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
            Checksum = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            InterpretationSummary = table.Column<string>(type: "TEXT", nullable: true),
            InterpretedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
            InterpretedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_evidence_attachments", x => x.Id);
            table.ForeignKey(
                      name: "FK_evidence_attachments_inspection_sessions_InspectionSessionId",
                      column: x => x.InspectionSessionId,
                      principalTable: "inspection_sessions",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "field_notes",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            InspectionSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
            CapturedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            CapturedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            RawText = table.Column<string>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_field_notes", x => x.Id);
            table.ForeignKey(
                      name: "FK_field_notes_inspection_sessions_InspectionSessionId",
                      column: x => x.InspectionSessionId,
                      principalTable: "inspection_sessions",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "reports",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
            InspectionSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
            Title = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
            Body = table.Column<string>(type: "TEXT", nullable: false),
            CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            Lifecycle = table.Column<int>(type: "INTEGER", nullable: false),
            ApprovedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
            ApprovedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_reports", x => x.Id);
            table.ForeignKey(
                      name: "FK_reports_inspection_sessions_InspectionSessionId",
                      column: x => x.InspectionSessionId,
                      principalTable: "inspection_sessions",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
            table.ForeignKey(
                      name: "FK_reports_projects_ProjectId",
                      column: x => x.ProjectId,
                      principalTable: "projects",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
          });

      migrationBuilder.CreateTable(
          name: "save_transactions",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            ReportId = table.Column<Guid>(type: "TEXT", nullable: false),
            InitiatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            State = table.Column<int>(type: "INTEGER", nullable: false),
            FailureReason = table.Column<string>(type: "TEXT", nullable: true),
            PreparedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
            PersistedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
            CompletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_save_transactions", x => x.Id);
            table.ForeignKey(
                      name: "FK_save_transactions_reports_ReportId",
                      column: x => x.ReportId,
                      principalTable: "reports",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
          });

      migrationBuilder.CreateIndex(
          name: "IX_audit_events_SubjectType_SubjectId_OccurredAtUtc",
          table: "audit_events",
          columns: AuditEventIndexColumns);

      migrationBuilder.CreateIndex(
          name: "IX_evidence_attachments_InspectionSessionId",
          table: "evidence_attachments",
          column: "InspectionSessionId");

      migrationBuilder.CreateIndex(
          name: "IX_field_notes_InspectionSessionId",
          table: "field_notes",
          column: "InspectionSessionId");

      migrationBuilder.CreateIndex(
          name: "IX_inspection_sessions_ProjectId",
          table: "inspection_sessions",
          column: "ProjectId");

      migrationBuilder.CreateIndex(
          name: "IX_reports_InspectionSessionId",
          table: "reports",
          column: "InspectionSessionId");

      migrationBuilder.CreateIndex(
          name: "IX_reports_ProjectId",
          table: "reports",
          column: "ProjectId");

      migrationBuilder.CreateIndex(
          name: "IX_save_transactions_ReportId",
          table: "save_transactions",
          column: "ReportId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "audit_events");

      migrationBuilder.DropTable(
          name: "evidence_attachments");

      migrationBuilder.DropTable(
          name: "field_notes");

      migrationBuilder.DropTable(
          name: "save_transactions");

      migrationBuilder.DropTable(
          name: "reports");

      migrationBuilder.DropTable(
          name: "inspection_sessions");

      migrationBuilder.DropTable(
          name: "projects");
    }
  }
}
