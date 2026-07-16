using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace SPINbuster.Infrastructure.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class AiDraftProposalSlice : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "ai_proposals",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            ModelRunId = table.Column<Guid>(type: "TEXT", nullable: false),
            ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
            InspectionSessionId = table.Column<Guid>(type: "TEXT", nullable: true),
            ReportId = table.Column<Guid>(type: "TEXT", nullable: false),
            ProviderId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            ModelName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            ModelDigest = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            PromptPackageId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            PromptPackageVersion = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
            OutputSchemaId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            OutputSchemaVersion = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
            ContextManifestId = table.Column<Guid>(type: "TEXT", nullable: false),
            ContextManifestHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            GeneratedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            LatencyMilliseconds = table.Column<long>(type: "INTEGER", nullable: true),
            InputTokenCount = table.Column<int>(type: "INTEGER", nullable: true),
            OutputTokenCount = table.Column<int>(type: "INTEGER", nullable: true),
            Temperature = table.Column<decimal>(type: "TEXT", nullable: true),
            ReferencedSourceIdsJson = table.Column<string>(type: "TEXT", nullable: false),
            StructuredPayloadJson = table.Column<string>(type: "TEXT", nullable: false),
            Status = table.Column<int>(type: "INTEGER", nullable: false),
            ConfidenceBand = table.Column<int>(type: "INTEGER", nullable: false),
            AbstentionReason = table.Column<string>(type: "TEXT", nullable: true),
            ReviewDispositionNotes = table.Column<string>(type: "TEXT", nullable: true),
            UncertaintyCodesJson = table.Column<string>(type: "TEXT", nullable: false),
            WarningsJson = table.Column<string>(type: "TEXT", nullable: false),
            ValidationFailuresJson = table.Column<string>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_ai_proposals", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "context_manifests",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
            InspectionSessionId = table.Column<Guid>(type: "TEXT", nullable: true),
            ContextPolicyVersion = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            Status = table.Column<int>(type: "INTEGER", nullable: false),
            ManifestHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            IncompleteReasonsJson = table.Column<string>(type: "TEXT", nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_context_manifests", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "model_runs",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
            InspectionSessionId = table.Column<Guid>(type: "TEXT", nullable: true),
            ReportId = table.Column<Guid>(type: "TEXT", nullable: false),
            InitiatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            ContextManifestId = table.Column<Guid>(type: "TEXT", nullable: false),
            ContextManifestHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            ProviderId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            ModelName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            ModelDigest = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            PromptPackageId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            PromptPackageVersion = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
            OutputSchemaId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            OutputSchemaVersion = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
            CorrelationId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            RequestFingerprintHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            RequestedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            State = table.Column<int>(type: "INTEGER", nullable: false),
            FailureClassification = table.Column<int>(type: "INTEGER", nullable: false),
            FailureMessage = table.Column<string>(type: "TEXT", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_model_runs", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "context_manifest_source_entries",
          columns: table => new
          {
            ContextManifestId = table.Column<Guid>(type: "TEXT", nullable: false),
            Order = table.Column<int>(type: "INTEGER", nullable: false),
            ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
            SourceType = table.Column<int>(type: "INTEGER", nullable: false),
            SourceId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            SourceVersion = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            ContentHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            AuthorityClassification = table.Column<int>(type: "INTEGER", nullable: false),
            InclusionReason = table.Column<string>(type: "TEXT", nullable: false),
            LimitationNotes = table.Column<string>(type: "TEXT", nullable: true),
            IsSuperseded = table.Column<bool>(type: "INTEGER", nullable: false),
            ConflictCodesJson = table.Column<string>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_context_manifest_source_entries", x => new { x.ContextManifestId, x.Order });
            table.ForeignKey(
                      name: "FK_context_manifest_source_entries_context_manifests_ContextManifestId",
                      column: x => x.ContextManifestId,
                      principalTable: "context_manifests",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "model_run_attempts",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            ModelRunId = table.Column<Guid>(type: "TEXT", nullable: false),
            AttemptNumber = table.Column<int>(type: "INTEGER", nullable: false),
            InputHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            StartedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            CompletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
            LatencyMilliseconds = table.Column<long>(type: "INTEGER", nullable: true),
            InputTokenCount = table.Column<int>(type: "INTEGER", nullable: true),
            OutputTokenCount = table.Column<int>(type: "INTEGER", nullable: true),
            RawOutput = table.Column<string>(type: "TEXT", nullable: true),
            RawOutputHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
            OutcomeClassification = table.Column<int>(type: "INTEGER", nullable: false),
            FailureMessage = table.Column<string>(type: "TEXT", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_model_run_attempts", x => x.Id);
            table.ForeignKey(
                      name: "FK_model_run_attempts_model_runs_ModelRunId",
                      column: x => x.ModelRunId,
                      principalTable: "model_runs",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateIndex(
          name: "IX_ai_proposals_ModelRunId",
          table: "ai_proposals",
          column: "ModelRunId",
          unique: true);

      migrationBuilder.CreateIndex(
          name: "IX_ai_proposals_ReportId",
          table: "ai_proposals",
          column: "ReportId");

      migrationBuilder.CreateIndex(
          name: "IX_context_manifest_source_entries_ProjectId_SourceType_SourceId",
          table: "context_manifest_source_entries",
          columns: new[] { "ProjectId", "SourceType", "SourceId" });

      migrationBuilder.CreateIndex(
          name: "IX_context_manifests_ManifestHash",
          table: "context_manifests",
          column: "ManifestHash",
          unique: true);

      migrationBuilder.CreateIndex(
          name: "IX_context_manifests_ProjectId",
          table: "context_manifests",
          column: "ProjectId");

      migrationBuilder.CreateIndex(
          name: "IX_model_run_attempts_ModelRunId_AttemptNumber",
          table: "model_run_attempts",
          columns: new[] { "ModelRunId", "AttemptNumber" },
          unique: true);

      migrationBuilder.CreateIndex(
          name: "IX_model_runs_ContextManifestId",
          table: "model_runs",
          column: "ContextManifestId");

      migrationBuilder.CreateIndex(
          name: "IX_model_runs_CorrelationId",
          table: "model_runs",
          column: "CorrelationId",
          unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "ai_proposals");

      migrationBuilder.DropTable(
          name: "context_manifest_source_entries");

      migrationBuilder.DropTable(
          name: "model_run_attempts");

      migrationBuilder.DropTable(
          name: "context_manifests");

      migrationBuilder.DropTable(
          name: "model_runs");
    }
  }
}
#pragma warning restore CA1861
