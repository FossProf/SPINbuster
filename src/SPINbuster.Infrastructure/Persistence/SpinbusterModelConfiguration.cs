using Microsoft.EntityFrameworkCore;
using SPINbuster.Domain;
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
    ConfigureKnowledgeEngine(modelBuilder);
    ConfigureDocumentEngine(modelBuilder);
    ConfigureParserRuns(modelBuilder);
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

  private static void ConfigureKnowledgeEngine(ModelBuilder modelBuilder)
  {
    var documentBuilder = modelBuilder.Entity<KnowledgeDocumentRecord>();
    documentBuilder.ToTable("knowledge_documents");
    documentBuilder.HasKey(record => record.Id);
    documentBuilder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.KnowledgeDocumentId).ValueGeneratedNever();
    documentBuilder.Property(record => record.ProjectId).HasConversion(StronglyTypedIdValueConverters.ProjectId).IsRequired();
    documentBuilder.Property(record => record.DocumentType).IsRequired();
    documentBuilder.Property(record => record.CanonicalTitle).HasMaxLength(512).IsRequired();
    documentBuilder.Property(record => record.ExternalReferenceNumber).HasMaxLength(256);
    documentBuilder.Property(record => record.DisciplineOrCategory).HasMaxLength(256);
    documentBuilder.Property(record => record.CurrentAuthoritativeRevisionId).HasConversion(StronglyTypedIdValueConverters.KnowledgeDocumentRevisionId);
    documentBuilder.Property(record => record.Lifecycle).IsRequired();
    documentBuilder.Property(record => record.CreatedBy).HasMaxLength(256).IsRequired();
    documentBuilder.Property(record => record.CreatedAtUtc).IsRequired();
    documentBuilder.HasIndex(record => record.ProjectId);
    documentBuilder.HasIndex(record => new { record.ProjectId, record.DocumentType });
    documentBuilder.HasIndex(record => record.CurrentAuthoritativeRevisionId).IsUnique();
    documentBuilder.HasMany(record => record.Revisions)
      .WithOne()
      .HasForeignKey(record => record.KnowledgeDocumentId)
      .OnDelete(DeleteBehavior.Cascade);
    documentBuilder.HasOne<ProjectRecord>()
      .WithMany()
      .HasForeignKey(record => record.ProjectId)
      .OnDelete(DeleteBehavior.Restrict);
    documentBuilder.HasOne<KnowledgeDocumentRevisionRecord>()
      .WithMany()
      .HasForeignKey(record => record.CurrentAuthoritativeRevisionId)
      .OnDelete(DeleteBehavior.Restrict);

    var revisionBuilder = modelBuilder.Entity<KnowledgeDocumentRevisionRecord>();
    revisionBuilder.ToTable("knowledge_document_revisions");
    revisionBuilder.HasKey(record => record.Id);
    revisionBuilder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.KnowledgeDocumentRevisionId).ValueGeneratedNever();
    revisionBuilder.Property(record => record.KnowledgeDocumentId).HasConversion(StronglyTypedIdValueConverters.KnowledgeDocumentId).IsRequired();
    revisionBuilder.Property(record => record.KnowledgeSourceId).HasConversion(StronglyTypedIdValueConverters.KnowledgeSourceId).IsRequired();
    revisionBuilder.Property(record => record.RevisionLabel).HasMaxLength(128).UseCollation("NOCASE").IsRequired();
    revisionBuilder.Property(record => record.EffectiveDate).IsRequired(false);
    revisionBuilder.Property(record => record.ReceivedAtUtc).IsRequired();
    revisionBuilder.Property(record => record.SourceAuthority).IsRequired();
    revisionBuilder.Property(record => record.VerificationStatus).IsRequired();
    revisionBuilder.Property(record => record.ContentHash).HasMaxLength(256).IsRequired();
    revisionBuilder.Property(record => record.MetadataHash).HasMaxLength(256).IsRequired();
    revisionBuilder.Property(record => record.SupersedesRevisionId).HasConversion(StronglyTypedIdValueConverters.KnowledgeDocumentRevisionId);
    revisionBuilder.Property(record => record.SupersededByRevisionId).HasConversion(StronglyTypedIdValueConverters.KnowledgeDocumentRevisionId);
    revisionBuilder.Property(record => record.SourceSystemReference).HasMaxLength(256);
    revisionBuilder.Property(record => record.DescriptiveNotes).HasColumnType("TEXT");
    revisionBuilder.Property(record => record.CreatedAtUtc).IsRequired();
    revisionBuilder.Property(record => record.IngestionStatus).IsRequired();
    revisionBuilder.Property(record => record.Lifecycle).IsRequired();
    revisionBuilder.HasIndex(record => record.KnowledgeDocumentId)
      .HasDatabaseName("IX_knowledge_document_revisions_KnowledgeDocumentId_CurrentAuthoritative")
      .HasFilter($"{nameof(KnowledgeDocumentRevisionRecord.Lifecycle)} = {(int)KnowledgeRevisionLifecycle.CurrentAuthoritative}")
      .IsUnique();
    revisionBuilder.HasIndex(record => new { record.KnowledgeDocumentId, record.RevisionLabel }).IsUnique();
    revisionBuilder.HasIndex(record => record.SupersedesRevisionId);
    revisionBuilder.HasIndex(record => record.SupersededByRevisionId);
    revisionBuilder.HasMany(record => record.Citations)
      .WithOne()
      .HasForeignKey(record => record.CitedRevisionId)
      .OnDelete(DeleteBehavior.Cascade);

    var relationshipBuilder = modelBuilder.Entity<KnowledgeRelationshipRecord>();
    relationshipBuilder.ToTable("knowledge_relationships");
    relationshipBuilder.HasKey(record => record.Id);
    relationshipBuilder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.KnowledgeRelationshipId).ValueGeneratedNever();
    relationshipBuilder.Property(record => record.ProjectId).HasConversion(StronglyTypedIdValueConverters.ProjectId).IsRequired();
    relationshipBuilder.Property(record => record.SourceKind).IsRequired();
    relationshipBuilder.Property(record => record.SourceKey).HasMaxLength(128).IsRequired();
    relationshipBuilder.Property(record => record.SourceDocumentId).HasConversion(StronglyTypedIdValueConverters.KnowledgeDocumentId);
    relationshipBuilder.Property(record => record.SourceRevisionId).HasConversion(StronglyTypedIdValueConverters.KnowledgeDocumentRevisionId);
    relationshipBuilder.Property(record => record.TargetKind).IsRequired();
    relationshipBuilder.Property(record => record.TargetKey).HasMaxLength(128).IsRequired();
    relationshipBuilder.Property(record => record.TargetDocumentId).HasConversion(StronglyTypedIdValueConverters.KnowledgeDocumentId);
    relationshipBuilder.Property(record => record.TargetRevisionId).HasConversion(StronglyTypedIdValueConverters.KnowledgeDocumentRevisionId);
    relationshipBuilder.Property(record => record.RelationshipType).IsRequired();
    relationshipBuilder.Property(record => record.EvidenceOrRationale).HasColumnType("TEXT").IsRequired();
    relationshipBuilder.Property(record => record.CreatedBy).HasMaxLength(256).IsRequired();
    relationshipBuilder.Property(record => record.CreatedAtUtc).IsRequired();
    relationshipBuilder.Property(record => record.CreatedAtUtcTicks).IsRequired();
    relationshipBuilder.Property(record => record.VerificationStatus).IsRequired();
    relationshipBuilder.HasIndex(record => new
    {
      record.ProjectId,
      record.SourceKey,
      record.TargetKey,
      record.RelationshipType,
    }).IsUnique();
    relationshipBuilder.HasIndex(record => new
    {
      record.ProjectId,
      record.SourceKey,
    });
    relationshipBuilder.HasIndex(record => new
    {
      record.ProjectId,
      record.TargetKey,
    });
    relationshipBuilder.HasOne<ProjectRecord>()
      .WithMany()
      .HasForeignKey(record => record.ProjectId)
      .OnDelete(DeleteBehavior.Restrict);

    var citationBuilder = modelBuilder.Entity<KnowledgeCitationRecord>();
    citationBuilder.ToTable("knowledge_citations");
    citationBuilder.HasKey(record => record.Id);
    citationBuilder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.KnowledgeCitationId).ValueGeneratedNever();
    citationBuilder.Property(record => record.CitedRevisionId).HasConversion(StronglyTypedIdValueConverters.KnowledgeDocumentRevisionId).IsRequired();
    citationBuilder.Property(record => record.LocatorType).IsRequired();
    citationBuilder.Property(record => record.LocatorValue).HasMaxLength(512).IsRequired();
    citationBuilder.Property(record => record.RevisionContentHash).HasMaxLength(256).IsRequired();
    citationBuilder.Property(record => record.CreatedAtUtc).IsRequired();
    citationBuilder.Property(record => record.QuotedOrSummarizedText).HasColumnType("TEXT");
    citationBuilder.HasIndex(record => record.CitedRevisionId);
    citationBuilder.HasIndex(record => new { record.CitedRevisionId, record.LocatorType, record.LocatorValue });
  }

  private static void ConfigureDocumentEngine(ModelBuilder modelBuilder)
  {
    var storageBuilder = modelBuilder.Entity<StorageObjectRecord>();
    storageBuilder.ToTable("storage_objects");
    storageBuilder.HasKey(record => record.Id);
    storageBuilder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.StorageObjectId).ValueGeneratedNever();
    storageBuilder.Property(record => record.StorageProviderKey).HasMaxLength(128).IsRequired();
    storageBuilder.Property(record => record.ImmutableObjectKey).HasMaxLength(512).IsRequired();
    storageBuilder.Property(record => record.ContentLength).IsRequired();
    storageBuilder.Property(record => record.ContentHash).HasMaxLength(128).IsRequired();
    storageBuilder.Property(record => record.HashAlgorithm).HasMaxLength(64).IsRequired();
    storageBuilder.Property(record => record.HashAlgorithmVersion).IsRequired();
    storageBuilder.Property(record => record.CreatedAtUtc).IsRequired();
    storageBuilder.Property(record => record.EncryptionMetadataId).HasMaxLength(256);
    storageBuilder.Property(record => record.AvailabilityState).IsRequired();
    storageBuilder.HasIndex(record => new { record.ContentHash, record.HashAlgorithm, record.HashAlgorithmVersion }).IsUnique();
    storageBuilder.HasIndex(record => new { record.StorageProviderKey, record.ImmutableObjectKey }).IsUnique();

    var sessionBuilder = modelBuilder.Entity<DocumentImportSessionRecord>();
    sessionBuilder.ToTable("document_import_sessions");
    sessionBuilder.HasKey(record => record.Id);
    sessionBuilder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.DocumentImportSessionId).ValueGeneratedNever();
    sessionBuilder.Property(record => record.ProjectId).HasConversion(StronglyTypedIdValueConverters.ProjectId).IsRequired();
    sessionBuilder.Property(record => record.InitiatedBy).HasMaxLength(256).IsRequired();
    sessionBuilder.Property(record => record.StartedAtUtc).IsRequired();
    sessionBuilder.Property(record => record.CompletedAtUtc).IsRequired(false);
    sessionBuilder.Property(record => record.State).IsRequired();
    sessionBuilder.Property(record => record.FailureSummary).HasColumnType("TEXT");
    sessionBuilder.HasIndex(record => record.ProjectId);
    sessionBuilder.HasIndex(record => record.State);

    var sourceBuilder = modelBuilder.Entity<ImportedDocumentSourceRecord>();
    sourceBuilder.ToTable("imported_document_sources");
    sourceBuilder.HasKey(record => record.Id);
    sourceBuilder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.ImportedSourceId).ValueGeneratedNever();
    sourceBuilder.Property(record => record.ImportSessionId).HasConversion(StronglyTypedIdValueConverters.DocumentImportSessionId).IsRequired();
    sourceBuilder.Property(record => record.ProjectId).HasConversion(StronglyTypedIdValueConverters.ProjectId).IsRequired();
    sourceBuilder.Property(record => record.OriginalFileName).HasMaxLength(512).IsRequired();
    sourceBuilder.Property(record => record.DeclaredMediaType).HasMaxLength(256);
    sourceBuilder.Property(record => record.DetectedMediaType).HasMaxLength(256);
    sourceBuilder.Property(record => record.ContentLength).IsRequired();
    sourceBuilder.Property(record => record.ContentHash).HasMaxLength(128).IsRequired();
    sourceBuilder.Property(record => record.HashAlgorithm).HasMaxLength(64).IsRequired();
    sourceBuilder.Property(record => record.HashAlgorithmVersion).IsRequired();
    sourceBuilder.Property(record => record.StorageObjectId).HasConversion(StronglyTypedIdValueConverters.StorageObjectId).IsRequired();
    sourceBuilder.Property(record => record.SourceOrigin).IsRequired();
    sourceBuilder.Property(record => record.ImportedBy).HasMaxLength(256).IsRequired();
    sourceBuilder.Property(record => record.ImportedAtUtc).IsRequired();
    sourceBuilder.Property(record => record.Status).IsRequired();
    sourceBuilder.Property(record => record.ExternalSourceReference).HasMaxLength(256);
    sourceBuilder.HasIndex(record => new { record.ProjectId, record.ContentHash, record.HashAlgorithm, record.HashAlgorithmVersion });
    sourceBuilder.HasIndex(record => record.ImportSessionId);
    sourceBuilder.HasIndex(record => record.StorageObjectId);
    sourceBuilder.HasOne<DocumentImportSessionRecord>().WithMany().HasForeignKey(record => record.ImportSessionId).OnDelete(DeleteBehavior.Restrict);
    sourceBuilder.HasOne<ProjectRecord>().WithMany().HasForeignKey(record => record.ProjectId).OnDelete(DeleteBehavior.Restrict);
    sourceBuilder.HasOne<StorageObjectRecord>().WithMany().HasForeignKey(record => record.StorageObjectId).OnDelete(DeleteBehavior.Restrict);

    var attemptBuilder = modelBuilder.Entity<DocumentProcessingAttemptRecord>();
    attemptBuilder.ToTable("document_processing_attempts");
    attemptBuilder.HasKey(record => record.Id);
    attemptBuilder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.DocumentProcessingAttemptId).ValueGeneratedNever();
    attemptBuilder.Property(record => record.ImportedSourceId).HasConversion(StronglyTypedIdValueConverters.ImportedSourceId).IsRequired();
    attemptBuilder.Property(record => record.ProjectId).HasConversion(StronglyTypedIdValueConverters.ProjectId).IsRequired();
    attemptBuilder.Property(record => record.ProcessorRole).HasMaxLength(128).IsRequired();
    attemptBuilder.Property(record => record.ProcessorIdentity).HasMaxLength(128).IsRequired();
    attemptBuilder.Property(record => record.ProcessorVersion).HasMaxLength(64).IsRequired();
    attemptBuilder.Property(record => record.RequestedAtUtc).IsRequired();
    attemptBuilder.Property(record => record.State).IsRequired();
    attemptBuilder.Property(record => record.FailureClassification).IsRequired();
    attemptBuilder.Property(record => record.FailureDetails).HasColumnType("TEXT");
    attemptBuilder.Property(record => record.InputContentHash).HasMaxLength(128).IsRequired();
    attemptBuilder.Property(record => record.OutputHash).HasMaxLength(128);
    attemptBuilder.HasIndex(record => new { record.ImportedSourceId, record.AttemptNumber }).IsUnique();
    attemptBuilder.HasIndex(record => record.ProjectId);
    attemptBuilder.HasOne<ImportedDocumentSourceRecord>().WithMany().HasForeignKey(record => record.ImportedSourceId).OnDelete(DeleteBehavior.Restrict);

    var candidateBuilder = modelBuilder.Entity<DocumentCandidateRecord>();
    candidateBuilder.ToTable("document_candidates");
    candidateBuilder.HasKey(record => record.Id);
    candidateBuilder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.DocumentCandidateId).ValueGeneratedNever();
    candidateBuilder.Property(record => record.ProjectId).HasConversion(StronglyTypedIdValueConverters.ProjectId).IsRequired();
    candidateBuilder.Property(record => record.ImportedSourceId).HasConversion(StronglyTypedIdValueConverters.ImportedSourceId).IsRequired();
    candidateBuilder.Property(record => record.ProcessingAttemptId).HasConversion(StronglyTypedIdValueConverters.DocumentProcessingAttemptId).IsRequired();
    candidateBuilder.Property(record => record.CandidateType).IsRequired();
    candidateBuilder.Property(record => record.SchemaId).HasMaxLength(128).IsRequired();
    candidateBuilder.Property(record => record.SchemaVersion).HasMaxLength(64).IsRequired();
    candidateBuilder.Property(record => record.PayloadHash).HasMaxLength(128).IsRequired();
    candidateBuilder.Property(record => record.CanonicalPayload).HasColumnType("TEXT").IsRequired();
    candidateBuilder.Property(record => record.SourceContentHash).HasMaxLength(128).IsRequired();
    candidateBuilder.Property(record => record.SourceLocator).HasMaxLength(512);
    candidateBuilder.Property(record => record.ConfidenceBand).IsRequired();
    candidateBuilder.Property(record => record.UncertaintyCodesJson).HasColumnType("TEXT").IsRequired();
    candidateBuilder.Property(record => record.Status).IsRequired();
    candidateBuilder.Property(record => record.CreatedAtUtc).IsRequired();
    candidateBuilder.Property(record => record.ReviewedBy).HasMaxLength(256);
    candidateBuilder.Property(record => record.ReviewNotes).HasColumnType("TEXT");
    candidateBuilder.HasIndex(record => record.ImportedSourceId);
    candidateBuilder.HasIndex(record => record.ProcessingAttemptId);
    candidateBuilder.HasIndex(record => new { record.SchemaId, record.SchemaVersion });
    candidateBuilder.HasIndex(record => record.Status);
    candidateBuilder.HasOne<ImportedDocumentSourceRecord>().WithMany().HasForeignKey(record => record.ImportedSourceId).OnDelete(DeleteBehavior.Restrict);
    candidateBuilder.HasOne<DocumentProcessingAttemptRecord>().WithMany().HasForeignKey(record => record.ProcessingAttemptId).OnDelete(DeleteBehavior.Restrict);
  }

  private static void ConfigureParserRuns(ModelBuilder modelBuilder)
  {
    var parserRunBuilder = modelBuilder.Entity<ParserRunRecord>();
    parserRunBuilder.ToTable("parser_runs");
    parserRunBuilder.HasKey(record => record.Id);
    parserRunBuilder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.ParserRunId).ValueGeneratedNever();
    parserRunBuilder.Property(record => record.ProjectId).HasConversion(StronglyTypedIdValueConverters.ProjectId).IsRequired();
    parserRunBuilder.Property(record => record.ImportedSourceId).HasConversion(StronglyTypedIdValueConverters.ImportedSourceId).IsRequired();
    parserRunBuilder.Property(record => record.ParserKey).HasMaxLength(128).IsRequired();
    parserRunBuilder.Property(record => record.ParserVersion).HasMaxLength(64).IsRequired();
    parserRunBuilder.Property(record => record.ParserContractVersion).HasMaxLength(64).IsRequired();
    parserRunBuilder.Property(record => record.ParserContractHash).HasMaxLength(128).IsRequired();
    parserRunBuilder.Property(record => record.SourceContentHash).HasMaxLength(128).IsRequired();
    parserRunBuilder.Property(record => record.SourceHashAlgorithm).HasMaxLength(64).IsRequired();
    parserRunBuilder.Property(record => record.SourceHashAlgorithmVersion).IsRequired();
    parserRunBuilder.Property(record => record.CreatedBy).HasMaxLength(256).IsRequired();
    parserRunBuilder.Property(record => record.CreatedAtUtc).IsRequired();
    parserRunBuilder.Property(record => record.State).IsRequired();
    parserRunBuilder.Property(record => record.FailureReason).HasColumnType("TEXT");
    parserRunBuilder.HasIndex(record => record.ProjectId);
    parserRunBuilder.HasIndex(record => record.ImportedSourceId);
    parserRunBuilder.HasIndex(record => new { record.ImportedSourceId, record.ParserKey, record.ParserVersion, record.ParserContractVersion, record.ParserContractHash }).IsUnique();
    parserRunBuilder.HasOne<ProjectRecord>().WithMany().HasForeignKey(record => record.ProjectId).OnDelete(DeleteBehavior.Restrict);
    parserRunBuilder.HasOne<ImportedDocumentSourceRecord>().WithMany().HasForeignKey(record => record.ImportedSourceId).OnDelete(DeleteBehavior.Restrict);

    var fragmentCandidateBuilder = modelBuilder.Entity<FragmentCandidateRecord>();
    fragmentCandidateBuilder.HasKey(record => record.Id);
    fragmentCandidateBuilder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.FragmentCandidateId).ValueGeneratedNever();
    fragmentCandidateBuilder.Property(record => record.ParserRunId).HasConversion(StronglyTypedIdValueConverters.ParserRunId).IsRequired();
    fragmentCandidateBuilder.Property(record => record.ProjectId).HasConversion(StronglyTypedIdValueConverters.ProjectId).IsRequired();
    fragmentCandidateBuilder.Property(record => record.ImportedSourceId).HasConversion(StronglyTypedIdValueConverters.ImportedSourceId).IsRequired();
    fragmentCandidateBuilder.Property(record => record.SourceContentHash).HasMaxLength(128).IsRequired();
    fragmentCandidateBuilder.Property(record => record.LocatorType).IsRequired();
    fragmentCandidateBuilder.Property(record => record.LocatorRawValue).HasMaxLength(512).IsRequired();
    fragmentCandidateBuilder.Property(record => record.LocatorNormalizedValue).HasMaxLength(512).IsRequired();
    fragmentCandidateBuilder.Property(record => record.Ordinal).IsRequired();
    fragmentCandidateBuilder.Property(record => record.ContentKind).IsRequired();
    fragmentCandidateBuilder.Property(record => record.ExtractedText).HasColumnType("TEXT").IsRequired();
    fragmentCandidateBuilder.Property(record => record.TextLength).IsRequired();
    fragmentCandidateBuilder.Property(record => record.ConfidenceBand).IsRequired();
    fragmentCandidateBuilder.Property(record => record.IdentityKey).HasMaxLength(1024).IsRequired();
    fragmentCandidateBuilder.Property(record => record.IdentityKeyHash).HasMaxLength(128).IsRequired();
    fragmentCandidateBuilder.Property(record => record.CreatedAtUtc).IsRequired();
    fragmentCandidateBuilder.Property(record => record.ReviewState).IsRequired();
    fragmentCandidateBuilder.Property(record => record.ReviewedBy).HasMaxLength(256);
    fragmentCandidateBuilder.Property(record => record.ReviewedAtUtc);
    fragmentCandidateBuilder.Property(record => record.ReviewNotes).HasMaxLength(2000);
    fragmentCandidateBuilder.HasIndex(record => record.ParserRunId);
    fragmentCandidateBuilder.HasIndex(record => record.ImportedSourceId);
    fragmentCandidateBuilder.HasIndex(record => record.IdentityKeyHash);
    fragmentCandidateBuilder.HasIndex(record => new { record.ProjectId, record.ReviewState }).HasDatabaseName("IX_parser_fragment_candidates_ProjectId_ReviewState");
    fragmentCandidateBuilder.HasIndex(record => new { record.ParserRunId, record.ReviewState }).HasDatabaseName("IX_parser_fragment_candidates_ParserRunId_ReviewState");
    fragmentCandidateBuilder.ToTable("parser_fragment_candidates", tableBuilder =>
      tableBuilder.HasCheckConstraint(
        "CK_parser_fragment_candidates_review_metadata",
        "(ReviewState = 0 AND ReviewedBy IS NULL AND ReviewedAtUtc IS NULL) OR (ReviewState IN (1, 2) AND ReviewedBy IS NOT NULL AND ReviewedAtUtc IS NOT NULL)"));
    fragmentCandidateBuilder.HasOne<ParserRunRecord>().WithMany().HasForeignKey(record => record.ParserRunId).OnDelete(DeleteBehavior.Restrict);
    fragmentCandidateBuilder.HasOne<ImportedDocumentSourceRecord>().WithMany().HasForeignKey(record => record.ImportedSourceId).OnDelete(DeleteBehavior.Restrict);
  }
}
