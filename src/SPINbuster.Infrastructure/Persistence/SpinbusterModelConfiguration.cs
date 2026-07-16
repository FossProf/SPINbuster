using Microsoft.EntityFrameworkCore;
using SPINbuster.Infrastructure.Persistence.Records;

namespace SPINbuster.Infrastructure.Persistence;

internal static class SpinbusterModelConfiguration
{
  public static void Configure(ModelBuilder modelBuilder)
  {
    modelBuilder.HasAnnotation("ProductVersion", "9.0.0");
    modelBuilder.HasAnnotation("Relational:MaxIdentifierLength", 128);

    ConfigureProjects(modelBuilder);
    ConfigureInspectionSessions(modelBuilder);
    ConfigureReports(modelBuilder);
    ConfigureSaveTransactions(modelBuilder);
    ConfigureAuditEvents(modelBuilder);
    ConfigureAiSubstrate(modelBuilder);
  }

  private static void ConfigureProjects(ModelBuilder modelBuilder)
  {
    var builder = modelBuilder.Entity<ProjectRecord>();
    builder.ToTable("projects");
    builder.HasKey(record => record.Id);
    builder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.ProjectId).ValueGeneratedNever();
    builder.Property(record => record.Name).HasMaxLength(256).IsRequired();
    builder.Property(record => record.CreatedBy).HasMaxLength(256).IsRequired();
    builder.Property(record => record.CreatedAtUtc).IsRequired();
    builder.Property(record => record.Lifecycle).IsRequired();
  }

  private static void ConfigureInspectionSessions(ModelBuilder modelBuilder)
  {
    var builder = modelBuilder.Entity<InspectionSessionRecord>();
    builder.ToTable("inspection_sessions");
    builder.HasKey(record => record.Id);
    builder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.InspectionSessionId).ValueGeneratedNever();
    builder.Property(record => record.ProjectId).HasConversion(StronglyTypedIdValueConverters.ProjectId).IsRequired();
    builder.Property(record => record.Name).HasMaxLength(256).IsRequired();
    builder.Property(record => record.CreatedBy).HasMaxLength(256).IsRequired();
    builder.Property(record => record.CreatedAtUtc).IsRequired();
    builder.Property(record => record.Lifecycle).IsRequired();
    builder.HasIndex(record => record.ProjectId);
    builder.HasOne<ProjectRecord>()
      .WithMany()
      .HasForeignKey(record => record.ProjectId)
      .OnDelete(DeleteBehavior.Restrict);

    builder.HasMany(record => record.FieldNotes)
      .WithOne()
      .HasForeignKey(record => record.InspectionSessionId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasMany(record => record.EvidenceAttachments)
      .WithOne()
      .HasForeignKey(record => record.InspectionSessionId)
      .OnDelete(DeleteBehavior.Cascade);

    var fieldNoteBuilder = modelBuilder.Entity<FieldNoteRecord>();
    fieldNoteBuilder.ToTable("field_notes");
    fieldNoteBuilder.HasKey(record => record.Id);
    fieldNoteBuilder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.FieldNoteId).ValueGeneratedNever();
    fieldNoteBuilder.Property(record => record.InspectionSessionId).HasConversion(StronglyTypedIdValueConverters.InspectionSessionId).IsRequired();
    fieldNoteBuilder.Property(record => record.CapturedBy).HasMaxLength(256).IsRequired();
    fieldNoteBuilder.Property(record => record.CapturedAtUtc).IsRequired();
    fieldNoteBuilder.Property(record => record.RawText).HasColumnType("TEXT").IsRequired();
    fieldNoteBuilder.HasIndex(record => record.InspectionSessionId);

    var evidenceBuilder = modelBuilder.Entity<EvidenceAttachmentRecord>();
    evidenceBuilder.ToTable("evidence_attachments");
    evidenceBuilder.HasKey(record => record.Id);
    evidenceBuilder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.EvidenceAttachmentId).ValueGeneratedNever();
    evidenceBuilder.Property(record => record.InspectionSessionId).HasConversion(StronglyTypedIdValueConverters.InspectionSessionId).IsRequired();
    evidenceBuilder.Property(record => record.CapturedBy).HasMaxLength(256).IsRequired();
    evidenceBuilder.Property(record => record.CapturedAtUtc).IsRequired();
    evidenceBuilder.Property(record => record.FileName).HasMaxLength(512).IsRequired();
    evidenceBuilder.Property(record => record.MediaType).HasMaxLength(256).IsRequired();
    evidenceBuilder.Property(record => record.StorageKey).HasMaxLength(1024).IsRequired();
    evidenceBuilder.Property(record => record.Checksum).HasMaxLength(256).IsRequired();
    evidenceBuilder.Property(record => record.InterpretationSummary).HasColumnType("TEXT");
    evidenceBuilder.Property(record => record.InterpretedBy).HasMaxLength(256);
    evidenceBuilder.HasIndex(record => record.InspectionSessionId);
  }

  private static void ConfigureReports(ModelBuilder modelBuilder)
  {
    var builder = modelBuilder.Entity<ReportRecord>();
    builder.ToTable("reports");
    builder.HasKey(record => record.Id);
    builder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.ReportId).ValueGeneratedNever();
    builder.Property(record => record.ProjectId).HasConversion(StronglyTypedIdValueConverters.ProjectId).IsRequired();
    builder.Property(record => record.InspectionSessionId).HasConversion(StronglyTypedIdValueConverters.InspectionSessionId).IsRequired();
    builder.Property(record => record.Title).HasMaxLength(512).IsRequired();
    builder.Property(record => record.RevisionNumber).IsRequired();
    builder.Property(record => record.CreatedBy).HasMaxLength(256).IsRequired();
    builder.Property(record => record.CreatedAtUtc).IsRequired();
    builder.Property(record => record.Lifecycle).IsRequired();
    builder.Property(record => record.ApprovedBy).HasMaxLength(256);
    builder.HasIndex(record => record.ProjectId);
    builder.HasIndex(record => record.InspectionSessionId);
    builder.HasOne<ProjectRecord>()
      .WithMany()
      .HasForeignKey(record => record.ProjectId)
      .OnDelete(DeleteBehavior.Restrict);
    builder.HasOne<InspectionSessionRecord>()
      .WithMany()
      .HasForeignKey(record => record.InspectionSessionId)
      .OnDelete(DeleteBehavior.Restrict);

    builder.HasMany(record => record.Sections)
      .WithOne()
      .HasForeignKey(record => record.ReportId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasMany(record => record.FieldNoteSources)
      .WithOne()
      .HasForeignKey(record => record.ReportId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasMany(record => record.EvidenceSources)
      .WithOne()
      .HasForeignKey(record => record.ReportId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasMany(record => record.Operations)
      .WithOne()
      .HasForeignKey(record => record.ReportId)
      .OnDelete(DeleteBehavior.Cascade);

    var sectionBuilder = modelBuilder.Entity<ReportSectionRecord>();
    sectionBuilder.ToTable("report_sections");
    sectionBuilder.HasKey(record => new { record.ReportId, record.Position });
    sectionBuilder.Property(record => record.ReportId).HasConversion(StronglyTypedIdValueConverters.ReportId).IsRequired();
    sectionBuilder.Property(record => record.Position).IsRequired();
    sectionBuilder.Property(record => record.Heading).HasMaxLength(256).IsRequired();
    sectionBuilder.Property(record => record.Content).HasColumnType("TEXT").IsRequired();

    var fieldNoteSourceBuilder = modelBuilder.Entity<ReportFieldNoteSourceRecord>();
    fieldNoteSourceBuilder.ToTable("report_field_note_sources");
    fieldNoteSourceBuilder.HasKey(record => new { record.ReportId, record.FieldNoteId });
    fieldNoteSourceBuilder.Property(record => record.ReportId).HasConversion(StronglyTypedIdValueConverters.ReportId).IsRequired();
    fieldNoteSourceBuilder.Property(record => record.FieldNoteId).HasConversion(StronglyTypedIdValueConverters.FieldNoteId).IsRequired();
    fieldNoteSourceBuilder.HasIndex(record => record.FieldNoteId);

    var evidenceSourceBuilder = modelBuilder.Entity<ReportEvidenceSourceRecord>();
    evidenceSourceBuilder.ToTable("report_evidence_sources");
    evidenceSourceBuilder.HasKey(record => new { record.ReportId, record.EvidenceAttachmentId });
    evidenceSourceBuilder.Property(record => record.ReportId).HasConversion(StronglyTypedIdValueConverters.ReportId).IsRequired();
    evidenceSourceBuilder.Property(record => record.EvidenceAttachmentId).HasConversion(StronglyTypedIdValueConverters.EvidenceAttachmentId).IsRequired();
    evidenceSourceBuilder.HasIndex(record => record.EvidenceAttachmentId);

    var operationBuilder = modelBuilder.Entity<ReportDraftOperationRecord>();
    operationBuilder.ToTable("report_draft_operations");
    operationBuilder.HasKey(record => record.OperationId);
    operationBuilder.Property(record => record.OperationId).HasConversion(StronglyTypedIdValueConverters.OperationId).ValueGeneratedNever();
    operationBuilder.Property(record => record.ReportId).HasConversion(StronglyTypedIdValueConverters.ReportId).IsRequired();
    operationBuilder.HasIndex(record => record.ReportId).IsUnique();
  }

  private static void ConfigureSaveTransactions(ModelBuilder modelBuilder)
  {
    var builder = modelBuilder.Entity<SaveTransactionRecord>();
    builder.ToTable("save_transactions");
    builder.HasKey(record => record.Id);
    builder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.SaveTransactionId).ValueGeneratedNever();
    builder.Property(record => record.ReportId).HasConversion(StronglyTypedIdValueConverters.ReportId).IsRequired();
    builder.Property(record => record.InitiatedBy).HasMaxLength(256).IsRequired();
    builder.Property(record => record.CreatedAtUtc).IsRequired();
    builder.Property(record => record.State).IsRequired();
    builder.Property(record => record.FailureReason).HasColumnType("TEXT");
    builder.HasIndex(record => record.ReportId);
    builder.HasOne<ReportRecord>()
      .WithMany()
      .HasForeignKey(record => record.ReportId)
      .OnDelete(DeleteBehavior.Restrict);
  }

  private static void ConfigureAuditEvents(ModelBuilder modelBuilder)
  {
    var builder = modelBuilder.Entity<AuditEventRecord>();
    builder.ToTable("audit_events");
    builder.HasKey(record => record.Id);
    builder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.AuditEventId).ValueGeneratedNever();
    builder.Property(record => record.SubjectType).HasMaxLength(128).IsRequired();
    builder.Property(record => record.SubjectId).HasMaxLength(64).IsRequired();
    builder.Property(record => record.EventType).HasMaxLength(128).IsRequired();
    builder.Property(record => record.Actor).HasMaxLength(256).IsRequired();
    builder.Property(record => record.OccurredAtUtc).IsRequired();
    builder.Property(record => record.Description).HasColumnType("TEXT").IsRequired();
    builder.HasIndex(record => new { record.SubjectType, record.SubjectId, record.OccurredAtUtc });
  }

  private static void ConfigureAiSubstrate(ModelBuilder modelBuilder)
  {
    var contextManifestBuilder = modelBuilder.Entity<ContextManifestRecord>();
    contextManifestBuilder.ToTable("context_manifests");
    contextManifestBuilder.HasKey(record => record.Id);
    contextManifestBuilder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.ContextManifestId).ValueGeneratedNever();
    contextManifestBuilder.Property(record => record.ProjectId).HasConversion(StronglyTypedIdValueConverters.ProjectId).IsRequired();
    contextManifestBuilder.Property(record => record.InspectionSessionId).HasConversion(StronglyTypedIdValueConverters.InspectionSessionId);
    contextManifestBuilder.Property(record => record.ContextPolicyVersion).HasMaxLength(128).IsRequired();
    contextManifestBuilder.Property(record => record.Status).IsRequired();
    contextManifestBuilder.Property(record => record.ManifestHash).HasMaxLength(128).IsRequired();
    contextManifestBuilder.Property(record => record.IncompleteReasonsJson).HasColumnType("TEXT").IsRequired();
    contextManifestBuilder.Property(record => record.CreatedAtUtc).IsRequired();
    contextManifestBuilder.HasIndex(record => record.ProjectId);
    contextManifestBuilder.HasIndex(record => record.ManifestHash).IsUnique();
    contextManifestBuilder.HasMany(record => record.Entries)
      .WithOne()
      .HasForeignKey(record => record.ContextManifestId)
      .OnDelete(DeleteBehavior.Cascade);

    var sourceEntryBuilder = modelBuilder.Entity<ContextManifestSourceEntryRecord>();
    sourceEntryBuilder.ToTable("context_manifest_source_entries");
    sourceEntryBuilder.HasKey(record => new { record.ContextManifestId, record.Order });
    sourceEntryBuilder.Property(record => record.ContextManifestId).HasConversion(StronglyTypedIdValueConverters.ContextManifestId).IsRequired();
    sourceEntryBuilder.Property(record => record.ProjectId).HasConversion(StronglyTypedIdValueConverters.ProjectId).IsRequired();
    sourceEntryBuilder.Property(record => record.SourceType).IsRequired();
    sourceEntryBuilder.Property(record => record.SourceId).HasMaxLength(128).IsRequired();
    sourceEntryBuilder.Property(record => record.SourceVersion).HasMaxLength(128).IsRequired();
    sourceEntryBuilder.Property(record => record.ContentHash).HasMaxLength(128).IsRequired();
    sourceEntryBuilder.Property(record => record.AuthorityClassification).IsRequired();
    sourceEntryBuilder.Property(record => record.InclusionReason).HasColumnType("TEXT").IsRequired();
    sourceEntryBuilder.Property(record => record.LimitationNotes).HasColumnType("TEXT");
    sourceEntryBuilder.Property(record => record.IsSuperseded).IsRequired();
    sourceEntryBuilder.Property(record => record.ConflictCodesJson).HasColumnType("TEXT").IsRequired();
    sourceEntryBuilder.HasIndex(record => new { record.ProjectId, record.SourceType, record.SourceId });

    var modelRunBuilder = modelBuilder.Entity<ModelRunRecord>();
    modelRunBuilder.ToTable("model_runs");
    modelRunBuilder.HasKey(record => record.Id);
    modelRunBuilder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.ModelRunId).ValueGeneratedNever();
    modelRunBuilder.Property(record => record.ProjectId).HasConversion(StronglyTypedIdValueConverters.ProjectId).IsRequired();
    modelRunBuilder.Property(record => record.InspectionSessionId).HasConversion(StronglyTypedIdValueConverters.InspectionSessionId);
    modelRunBuilder.Property(record => record.ReportId).HasConversion(StronglyTypedIdValueConverters.ReportId).IsRequired();
    modelRunBuilder.Property(record => record.InitiatedBy).HasMaxLength(256).IsRequired();
    modelRunBuilder.Property(record => record.ContextManifestId).HasConversion(StronglyTypedIdValueConverters.ContextManifestId).IsRequired();
    modelRunBuilder.Property(record => record.ContextManifestHash).HasMaxLength(128).IsRequired();
    modelRunBuilder.Property(record => record.ProviderId).HasMaxLength(128).IsRequired();
    modelRunBuilder.Property(record => record.ModelName).HasMaxLength(256).IsRequired();
    modelRunBuilder.Property(record => record.ModelDigest).HasMaxLength(256).IsRequired();
    modelRunBuilder.Property(record => record.PromptPackageId).HasMaxLength(128).IsRequired();
    modelRunBuilder.Property(record => record.PromptPackageVersion).HasMaxLength(64).IsRequired();
    modelRunBuilder.Property(record => record.OutputSchemaId).HasMaxLength(128).IsRequired();
    modelRunBuilder.Property(record => record.OutputSchemaVersion).HasMaxLength(64).IsRequired();
    modelRunBuilder.Property(record => record.CorrelationId).HasMaxLength(128).IsRequired();
    modelRunBuilder.Property(record => record.RequestFingerprintHash).HasMaxLength(128).IsRequired();
    modelRunBuilder.Property(record => record.RequestedAtUtc).IsRequired();
    modelRunBuilder.Property(record => record.State).IsRequired();
    modelRunBuilder.Property(record => record.FailureClassification).IsRequired();
    modelRunBuilder.Property(record => record.FailureMessage).HasColumnType("TEXT");
    modelRunBuilder.HasIndex(record => record.CorrelationId).IsUnique();
    modelRunBuilder.HasIndex(record => record.ContextManifestId);
    modelRunBuilder.HasMany(record => record.Attempts)
      .WithOne()
      .HasForeignKey(record => record.ModelRunId)
      .OnDelete(DeleteBehavior.Cascade);

    var attemptBuilder = modelBuilder.Entity<ModelRunAttemptRecord>();
    attemptBuilder.ToTable("model_run_attempts");
    attemptBuilder.HasKey(record => record.Id);
    attemptBuilder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.ModelRunAttemptId).ValueGeneratedNever();
    attemptBuilder.Property(record => record.ModelRunId).HasConversion(StronglyTypedIdValueConverters.ModelRunId).IsRequired();
    attemptBuilder.Property(record => record.AttemptNumber).IsRequired();
    attemptBuilder.Property(record => record.InputHash).HasMaxLength(128).IsRequired();
    attemptBuilder.Property(record => record.StartedAtUtc).IsRequired();
    attemptBuilder.Property(record => record.CompletedAtUtc).IsRequired(false);
    attemptBuilder.Property(record => record.RawOutput).HasColumnType("TEXT");
    attemptBuilder.Property(record => record.RawOutputHash).HasMaxLength(128);
    attemptBuilder.Property(record => record.FailureMessage).HasColumnType("TEXT");
    attemptBuilder.HasIndex(record => new { record.ModelRunId, record.AttemptNumber }).IsUnique();

    var proposalBuilder = modelBuilder.Entity<AiProposalRecord>();
    proposalBuilder.ToTable("ai_proposals");
    proposalBuilder.HasKey(record => record.Id);
    proposalBuilder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.ProposalId).ValueGeneratedNever();
    proposalBuilder.Property(record => record.ModelRunId).HasConversion(StronglyTypedIdValueConverters.ModelRunId).IsRequired();
    proposalBuilder.Property(record => record.ProjectId).HasConversion(StronglyTypedIdValueConverters.ProjectId).IsRequired();
    proposalBuilder.Property(record => record.InspectionSessionId).HasConversion(StronglyTypedIdValueConverters.InspectionSessionId);
    proposalBuilder.Property(record => record.ReportId).HasConversion(StronglyTypedIdValueConverters.ReportId).IsRequired();
    proposalBuilder.Property(record => record.ProviderId).HasMaxLength(128).IsRequired();
    proposalBuilder.Property(record => record.ModelName).HasMaxLength(256).IsRequired();
    proposalBuilder.Property(record => record.ModelDigest).HasMaxLength(256).IsRequired();
    proposalBuilder.Property(record => record.PromptPackageId).HasMaxLength(128).IsRequired();
    proposalBuilder.Property(record => record.PromptPackageVersion).HasMaxLength(64).IsRequired();
    proposalBuilder.Property(record => record.OutputSchemaId).HasMaxLength(128).IsRequired();
    proposalBuilder.Property(record => record.OutputSchemaVersion).HasMaxLength(64).IsRequired();
    proposalBuilder.Property(record => record.ContextManifestId).HasConversion(StronglyTypedIdValueConverters.ContextManifestId).IsRequired();
    proposalBuilder.Property(record => record.ContextManifestHash).HasMaxLength(128).IsRequired();
    proposalBuilder.Property(record => record.GeneratedAtUtc).IsRequired();
    proposalBuilder.Property(record => record.ReferencedSourceIdsJson).HasColumnType("TEXT").IsRequired();
    proposalBuilder.Property(record => record.StructuredPayloadJson).HasColumnType("TEXT").IsRequired();
    proposalBuilder.Property(record => record.Status).IsRequired();
    proposalBuilder.Property(record => record.ConfidenceBand).IsRequired();
    proposalBuilder.Property(record => record.AbstentionReason).HasColumnType("TEXT");
    proposalBuilder.Property(record => record.ReviewDispositionNotes).HasColumnType("TEXT");
    proposalBuilder.Property(record => record.UncertaintyCodesJson).HasColumnType("TEXT").IsRequired();
    proposalBuilder.Property(record => record.WarningsJson).HasColumnType("TEXT").IsRequired();
    proposalBuilder.Property(record => record.ValidationFailuresJson).HasColumnType("TEXT").IsRequired();
    proposalBuilder.HasIndex(record => record.ModelRunId).IsUnique();
    proposalBuilder.HasIndex(record => record.ReportId);
  }
}
