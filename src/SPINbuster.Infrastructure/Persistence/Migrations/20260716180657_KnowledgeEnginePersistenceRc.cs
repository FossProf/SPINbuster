using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861

namespace SPINbuster.Infrastructure.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class KnowledgeEnginePersistenceRc : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "knowledge_documents",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
            DocumentType = table.Column<int>(type: "INTEGER", nullable: false),
            CanonicalTitle = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
            ExternalReferenceNumber = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
            DisciplineOrCategory = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
            CurrentAuthoritativeRevisionId = table.Column<Guid>(type: "TEXT", nullable: true),
            Lifecycle = table.Column<int>(type: "INTEGER", nullable: false),
            CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_knowledge_documents", x => x.Id);
            table.ForeignKey(
                      name: "FK_knowledge_documents_projects_ProjectId",
                      column: x => x.ProjectId,
                      principalTable: "projects",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
          });

      migrationBuilder.CreateTable(
          name: "knowledge_relationships",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
            SourceKind = table.Column<int>(type: "INTEGER", nullable: false),
            SourceKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            SourceDocumentId = table.Column<Guid>(type: "TEXT", nullable: true),
            SourceRevisionId = table.Column<Guid>(type: "TEXT", nullable: true),
            TargetKind = table.Column<int>(type: "INTEGER", nullable: false),
            TargetKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
            TargetDocumentId = table.Column<Guid>(type: "TEXT", nullable: true),
            TargetRevisionId = table.Column<Guid>(type: "TEXT", nullable: true),
            RelationshipType = table.Column<int>(type: "INTEGER", nullable: false),
            EvidenceOrRationale = table.Column<string>(type: "TEXT", nullable: false),
            CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            VerificationStatus = table.Column<int>(type: "INTEGER", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_knowledge_relationships", x => x.Id);
            table.ForeignKey(
                      name: "FK_knowledge_relationships_projects_ProjectId",
                      column: x => x.ProjectId,
                      principalTable: "projects",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
          });

      migrationBuilder.CreateTable(
          name: "knowledge_document_revisions",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            KnowledgeDocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
            KnowledgeSourceId = table.Column<Guid>(type: "TEXT", nullable: false),
            RevisionLabel = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false, collation: "NOCASE"),
            EffectiveDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
            ReceivedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            SourceAuthority = table.Column<int>(type: "INTEGER", nullable: false),
            VerificationStatus = table.Column<int>(type: "INTEGER", nullable: false),
            ContentHash = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            MetadataHash = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            SupersedesRevisionId = table.Column<Guid>(type: "TEXT", nullable: true),
            SupersededByRevisionId = table.Column<Guid>(type: "TEXT", nullable: true),
            SourceSystemReference = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
            DescriptiveNotes = table.Column<string>(type: "TEXT", nullable: true),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            IngestionStatus = table.Column<int>(type: "INTEGER", nullable: false),
            Lifecycle = table.Column<int>(type: "INTEGER", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_knowledge_document_revisions", x => x.Id);
            table.ForeignKey(
                      name: "FK_knowledge_document_revisions_knowledge_documents_KnowledgeDocumentId",
                      column: x => x.KnowledgeDocumentId,
                      principalTable: "knowledge_documents",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "knowledge_citations",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            CitedRevisionId = table.Column<Guid>(type: "TEXT", nullable: false),
            LocatorType = table.Column<int>(type: "INTEGER", nullable: false),
            LocatorValue = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
            RevisionContentHash = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            QuotedOrSummarizedText = table.Column<string>(type: "TEXT", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_knowledge_citations", x => x.Id);
            table.ForeignKey(
                      name: "FK_knowledge_citations_knowledge_document_revisions_CitedRevisionId",
                      column: x => x.CitedRevisionId,
                      principalTable: "knowledge_document_revisions",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateIndex(
          name: "IX_knowledge_citations_CitedRevisionId",
          table: "knowledge_citations",
          column: "CitedRevisionId");

      migrationBuilder.CreateIndex(
          name: "IX_knowledge_citations_CitedRevisionId_LocatorType_LocatorValue",
          table: "knowledge_citations",
          columns: new[] { "CitedRevisionId", "LocatorType", "LocatorValue" });

      migrationBuilder.CreateIndex(
          name: "IX_knowledge_document_revisions_KnowledgeDocumentId",
          table: "knowledge_document_revisions",
          column: "KnowledgeDocumentId");

      migrationBuilder.CreateIndex(
          name: "IX_knowledge_document_revisions_KnowledgeDocumentId_Lifecycle",
          table: "knowledge_document_revisions",
          columns: new[] { "KnowledgeDocumentId", "Lifecycle" });

      migrationBuilder.CreateIndex(
          name: "IX_knowledge_document_revisions_KnowledgeDocumentId_RevisionLabel",
          table: "knowledge_document_revisions",
          columns: new[] { "KnowledgeDocumentId", "RevisionLabel" },
          unique: true);

      migrationBuilder.CreateIndex(
          name: "IX_knowledge_document_revisions_SupersededByRevisionId",
          table: "knowledge_document_revisions",
          column: "SupersededByRevisionId");

      migrationBuilder.CreateIndex(
          name: "IX_knowledge_document_revisions_SupersedesRevisionId",
          table: "knowledge_document_revisions",
          column: "SupersedesRevisionId");

      migrationBuilder.CreateIndex(
          name: "IX_knowledge_documents_ProjectId",
          table: "knowledge_documents",
          column: "ProjectId");

      migrationBuilder.CreateIndex(
          name: "IX_knowledge_documents_ProjectId_DocumentType",
          table: "knowledge_documents",
          columns: new[] { "ProjectId", "DocumentType" });

      migrationBuilder.CreateIndex(
          name: "IX_knowledge_relationships_ProjectId_SourceKey",
          table: "knowledge_relationships",
          columns: new[] { "ProjectId", "SourceKey" });

      migrationBuilder.CreateIndex(
          name: "IX_knowledge_relationships_ProjectId_SourceKey_TargetKey_RelationshipType",
          table: "knowledge_relationships",
          columns: new[] { "ProjectId", "SourceKey", "TargetKey", "RelationshipType" },
          unique: true);

      migrationBuilder.CreateIndex(
          name: "IX_knowledge_relationships_ProjectId_TargetKey",
          table: "knowledge_relationships",
          columns: new[] { "ProjectId", "TargetKey" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "knowledge_citations");

      migrationBuilder.DropTable(
          name: "knowledge_relationships");

      migrationBuilder.DropTable(
          name: "knowledge_document_revisions");

      migrationBuilder.DropTable(
          name: "knowledge_documents");
    }
  }
}

#pragma warning restore CA1861
