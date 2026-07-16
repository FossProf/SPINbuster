using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861

namespace SPINbuster.Infrastructure.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class DocumentEngineFoundationRc : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "document_import_sessions",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
            InitiatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            StartedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            CompletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
            State = table.Column<int>(type: "INTEGER", nullable: false),
            SourceCount = table.Column<int>(type: "INTEGER", nullable: false),
            AcceptedCount = table.Column<int>(type: "INTEGER", nullable: false),
            DuplicateCount = table.Column<int>(type: "INTEGER", nullable: false),
            RejectedCount = table.Column<int>(type: "INTEGER", nullable: false),
            FailureSummary = table.Column<string>(type: "TEXT", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_document_import_sessions", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "storage_objects",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            StorageProviderKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            ImmutableObjectKey = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
            ContentLength = table.Column<long>(type: "INTEGER", nullable: false),
            ContentHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            HashAlgorithm = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
            HashAlgorithmVersion = table.Column<int>(type: "INTEGER", nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            EncryptionMetadataId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
            AvailabilityState = table.Column<int>(type: "INTEGER", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_storage_objects", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "imported_document_sources",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            ImportSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
            ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
            OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
            DeclaredMediaType = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
            DetectedMediaType = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
            ContentLength = table.Column<long>(type: "INTEGER", nullable: false),
            ContentHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            HashAlgorithm = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
            HashAlgorithmVersion = table.Column<int>(type: "INTEGER", nullable: false),
            StorageObjectId = table.Column<Guid>(type: "TEXT", nullable: false),
            SourceOrigin = table.Column<int>(type: "INTEGER", nullable: false),
            ImportedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            ImportedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            Status = table.Column<int>(type: "INTEGER", nullable: false),
            ExternalSourceReference = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_imported_document_sources", x => x.Id);
            table.ForeignKey(
                      name: "FK_imported_document_sources_document_import_sessions_ImportSessionId",
                      column: x => x.ImportSessionId,
                      principalTable: "document_import_sessions",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
            table.ForeignKey(
                      name: "FK_imported_document_sources_projects_ProjectId",
                      column: x => x.ProjectId,
                      principalTable: "projects",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
            table.ForeignKey(
                      name: "FK_imported_document_sources_storage_objects_StorageObjectId",
                      column: x => x.StorageObjectId,
                      principalTable: "storage_objects",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
          });

      migrationBuilder.CreateTable(
          name: "document_processing_attempts",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            ImportedSourceId = table.Column<Guid>(type: "TEXT", nullable: false),
            ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
            ProcessorRole = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            ProcessorIdentity = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            ProcessorVersion = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
            RequestedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            StartedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
            CompletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
            AttemptNumber = table.Column<int>(type: "INTEGER", nullable: false),
            State = table.Column<int>(type: "INTEGER", nullable: false),
            FailureClassification = table.Column<int>(type: "INTEGER", nullable: false),
            FailureDetails = table.Column<string>(type: "TEXT", nullable: true),
            InputContentHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            OutputHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_document_processing_attempts", x => x.Id);
            table.ForeignKey(
                      name: "FK_document_processing_attempts_imported_document_sources_ImportedSourceId",
                      column: x => x.ImportedSourceId,
                      principalTable: "imported_document_sources",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
          });

      migrationBuilder.CreateTable(
          name: "document_candidates",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
            ImportedSourceId = table.Column<Guid>(type: "TEXT", nullable: false),
            ProcessingAttemptId = table.Column<Guid>(type: "TEXT", nullable: false),
            CandidateType = table.Column<int>(type: "INTEGER", nullable: false),
            SchemaId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            SchemaVersion = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
            PayloadHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            CanonicalPayload = table.Column<string>(type: "TEXT", nullable: false),
            SourceContentHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            SourceLocator = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
            ConfidenceBand = table.Column<int>(type: "INTEGER", nullable: false),
            UncertaintyCodesJson = table.Column<string>(type: "TEXT", nullable: false),
            Status = table.Column<int>(type: "INTEGER", nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            ReviewedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
            ReviewedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
            ReviewNotes = table.Column<string>(type: "TEXT", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_document_candidates", x => x.Id);
            table.ForeignKey(
                      name: "FK_document_candidates_document_processing_attempts_ProcessingAttemptId",
                      column: x => x.ProcessingAttemptId,
                      principalTable: "document_processing_attempts",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
            table.ForeignKey(
                      name: "FK_document_candidates_imported_document_sources_ImportedSourceId",
                      column: x => x.ImportedSourceId,
                      principalTable: "imported_document_sources",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
          });

      migrationBuilder.CreateIndex(
          name: "IX_document_candidates_ImportedSourceId",
          table: "document_candidates",
          column: "ImportedSourceId");

      migrationBuilder.CreateIndex(
          name: "IX_document_candidates_ProcessingAttemptId",
          table: "document_candidates",
          column: "ProcessingAttemptId");

      migrationBuilder.CreateIndex(
          name: "IX_document_candidates_SchemaId_SchemaVersion",
          table: "document_candidates",
          columns: new[] { "SchemaId", "SchemaVersion" });

      migrationBuilder.CreateIndex(
          name: "IX_document_candidates_Status",
          table: "document_candidates",
          column: "Status");

      migrationBuilder.CreateIndex(
          name: "IX_document_import_sessions_ProjectId",
          table: "document_import_sessions",
          column: "ProjectId");

      migrationBuilder.CreateIndex(
          name: "IX_document_import_sessions_State",
          table: "document_import_sessions",
          column: "State");

      migrationBuilder.CreateIndex(
          name: "IX_document_processing_attempts_ImportedSourceId_AttemptNumber",
          table: "document_processing_attempts",
          columns: new[] { "ImportedSourceId", "AttemptNumber" },
          unique: true);

      migrationBuilder.CreateIndex(
          name: "IX_document_processing_attempts_ProjectId",
          table: "document_processing_attempts",
          column: "ProjectId");

      migrationBuilder.CreateIndex(
          name: "IX_imported_document_sources_ImportSessionId",
          table: "imported_document_sources",
          column: "ImportSessionId");

      migrationBuilder.CreateIndex(
          name: "IX_imported_document_sources_ProjectId_ContentHash_HashAlgorithm_HashAlgorithmVersion",
          table: "imported_document_sources",
          columns: new[] { "ProjectId", "ContentHash", "HashAlgorithm", "HashAlgorithmVersion" });

      migrationBuilder.CreateIndex(
          name: "IX_imported_document_sources_StorageObjectId",
          table: "imported_document_sources",
          column: "StorageObjectId");

      migrationBuilder.CreateIndex(
          name: "IX_storage_objects_ContentHash_HashAlgorithm_HashAlgorithmVersion",
          table: "storage_objects",
          columns: new[] { "ContentHash", "HashAlgorithm", "HashAlgorithmVersion" },
          unique: true);

      migrationBuilder.CreateIndex(
          name: "IX_storage_objects_StorageProviderKey_ImmutableObjectKey",
          table: "storage_objects",
          columns: new[] { "StorageProviderKey", "ImmutableObjectKey" },
          unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "document_candidates");

      migrationBuilder.DropTable(
          name: "document_processing_attempts");

      migrationBuilder.DropTable(
          name: "imported_document_sources");

      migrationBuilder.DropTable(
          name: "document_import_sessions");

      migrationBuilder.DropTable(
          name: "storage_objects");
    }
  }
}

#pragma warning restore CA1861
