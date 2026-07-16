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

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    SpinbusterModelConfiguration.Configure(modelBuilder);
  }
}
