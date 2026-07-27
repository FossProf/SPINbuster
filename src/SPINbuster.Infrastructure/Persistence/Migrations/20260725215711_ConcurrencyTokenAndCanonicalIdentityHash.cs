using Microsoft.EntityFrameworkCore.Migrations;

#pragma warning disable CA1861

#nullable disable

namespace SPINbuster.Infrastructure.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class ConcurrencyTokenAndCanonicalIdentityHash : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<string>(
          name: "CanonicalIdentityHash",
          table: "knowledge_documents",
          type: "TEXT",
          maxLength: 128,
          nullable: true);

      migrationBuilder.Sql(@"
                UPDATE knowledge_documents
                SET CanonicalIdentityHash = sha256_hex(
                    ProjectId || '|' ||
                    CASE DocumentType
                        WHEN 0 THEN 'Drawing'
                        WHEN 1 THEN 'Specification'
                        WHEN 2 THEN 'RFI'
                        WHEN 3 THEN 'Bulletin'
                        WHEN 4 THEN 'Submittal'
                        WHEN 5 THEN 'ChangeOrder'
                        WHEN 6 THEN 'Report'
                        WHEN 7 THEN 'FieldNote'
                        WHEN 8 THEN 'Evidence'
                        WHEN 9 THEN 'GeneralReference'
                        ELSE 'Unknown'
                    END || '|' ||
                    UPPER(TRIM(CanonicalTitle)) || '|' ||
                    UPPER(TRIM(COALESCE(ExternalReferenceNumber, ''))) || '|' ||
                    UPPER(TRIM(COALESCE(DisciplineOrCategory, '')))
                )
                WHERE CanonicalIdentityHash IS NULL");

      migrationBuilder.AlterColumn<string>(
          name: "CanonicalIdentityHash",
          table: "knowledge_documents",
          type: "TEXT",
          maxLength: 128,
          nullable: false,
          defaultValue: "");

      migrationBuilder.CreateIndex(
          name: "IX_knowledge_documents_ProjectId_CanonicalIdentityHash",
          table: "knowledge_documents",
          columns: new[] { "ProjectId", "CanonicalIdentityHash" },
          unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropIndex(
          name: "IX_knowledge_documents_ProjectId_CanonicalIdentityHash",
          table: "knowledge_documents");

      migrationBuilder.DropColumn(
          name: "CanonicalIdentityHash",
          table: "knowledge_documents");
    }
  }
}
