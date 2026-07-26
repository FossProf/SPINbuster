using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SPINbuster.Infrastructure.Persistence.Records;

namespace SPINbuster.Infrastructure.Persistence;

public sealed class SpinbusterDbContext : DbContext
{
  public SpinbusterDbContext(DbContextOptions<SpinbusterDbContext> options)
    : base(options)
  {
  }

  internal static void RegisterSha256Hex(SqliteConnection connection)
  {
    connection.CreateFunction<string, string>(
      "sha256_hex",
      input =>
      {
        var bytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToHexString(SHA256.HashData(bytes));
      });
  }

  internal static async Task MigrateWithSha256Async(SpinbusterDbContext dbContext, CancellationToken cancellationToken = default)
  {
    var connection = (SqliteConnection)dbContext.Database.GetDbConnection();

    connection.StateChange += (_, args) =>
    {
      if (args.CurrentState == System.Data.ConnectionState.Open)
      {
        RegisterSha256Hex(connection);
      }
    };

    RegisterSha256Hex(connection);
    await dbContext.Database.MigrateAsync(cancellationToken);
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    SpinbusterModelConfiguration.Configure(modelBuilder);
  }

  internal DbSet<ProjectRecord> Projects => Set<ProjectRecord>();

  internal DbSet<InspectionSessionRecord> InspectionSessions => Set<InspectionSessionRecord>();

  internal DbSet<FieldNoteRecord> FieldNotes => Set<FieldNoteRecord>();

  internal DbSet<EvidenceAttachmentRecord> EvidenceAttachments => Set<EvidenceAttachmentRecord>();

  internal DbSet<ReportRecord> Reports => Set<ReportRecord>();

  internal DbSet<ReportSectionRecord> ReportSections => Set<ReportSectionRecord>();

  internal DbSet<ReportFieldNoteSourceRecord> ReportFieldNoteSources => Set<ReportFieldNoteSourceRecord>();

  internal DbSet<ReportEvidenceSourceRecord> ReportEvidenceSources => Set<ReportEvidenceSourceRecord>();

  internal DbSet<ReportDraftOperationRecord> ReportDraftOperations => Set<ReportDraftOperationRecord>();

  internal DbSet<SaveTransactionRecord> SaveTransactions => Set<SaveTransactionRecord>();

  internal DbSet<AuditEventRecord> AuditEvents => Set<AuditEventRecord>();

  internal DbSet<ContextManifestRecord> ContextManifests => Set<ContextManifestRecord>();

  internal DbSet<ContextManifestSourceEntryRecord> ContextManifestSourceEntries => Set<ContextManifestSourceEntryRecord>();

  internal DbSet<ModelRunRecord> ModelRuns => Set<ModelRunRecord>();

  internal DbSet<ModelRunAttemptRecord> ModelRunAttempts => Set<ModelRunAttemptRecord>();

  internal DbSet<AiProposalRecord> AiProposals => Set<AiProposalRecord>();

  internal DbSet<KnowledgeDocumentRecord> KnowledgeDocuments => Set<KnowledgeDocumentRecord>();

  internal DbSet<KnowledgeDocumentRevisionRecord> KnowledgeDocumentRevisions => Set<KnowledgeDocumentRevisionRecord>();

  internal DbSet<KnowledgeRelationshipRecord> KnowledgeRelationships => Set<KnowledgeRelationshipRecord>();

  internal DbSet<KnowledgeCitationRecord> KnowledgeCitations => Set<KnowledgeCitationRecord>();

  internal DbSet<StorageObjectRecord> StorageObjects => Set<StorageObjectRecord>();

  internal DbSet<ImportedDocumentSourceRecord> ImportedDocumentSources => Set<ImportedDocumentSourceRecord>();

  internal DbSet<DocumentImportSessionRecord> DocumentImportSessions => Set<DocumentImportSessionRecord>();

  internal DbSet<DocumentProcessingAttemptRecord> DocumentProcessingAttempts => Set<DocumentProcessingAttemptRecord>();

  internal DbSet<DocumentCandidateRecord> DocumentCandidates => Set<DocumentCandidateRecord>();

  internal DbSet<ParserRunRecord> ParserRuns => Set<ParserRunRecord>();

  internal DbSet<FragmentCandidateRecord> FragmentCandidates => Set<FragmentCandidateRecord>();

  internal DbSet<ParserDiagnosticRecord> ParserDiagnostics => Set<ParserDiagnosticRecord>();

  internal DbSet<PromotionDiagnosticRecord> PromotionDiagnostics => Set<PromotionDiagnosticRecord>();

  internal DbSet<PromotionAttemptRecord> PromotionAttempts => Set<PromotionAttemptRecord>();

  internal DbSet<PromotionProvenanceRecord> PromotionProvenances => Set<PromotionProvenanceRecord>();
}
