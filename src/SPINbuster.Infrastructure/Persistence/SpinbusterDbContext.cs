using Microsoft.EntityFrameworkCore;
using SPINbuster.Infrastructure.Persistence.Records;

namespace SPINbuster.Infrastructure.Persistence;

public sealed class SpinbusterDbContext : DbContext
{
  public SpinbusterDbContext(DbContextOptions<SpinbusterDbContext> options)
    : base(options)
  {
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

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    SpinbusterModelConfiguration.Configure(modelBuilder);
  }
}
