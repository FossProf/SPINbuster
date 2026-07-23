using System.Text.Json;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence.Records;

namespace SPINbuster.Infrastructure.Persistence;

internal static class InfrastructureMapper
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public static Project ToDomain(ProjectRecord record, IReadOnlyCollection<AuditEvent> auditTrail)
  {
    return Project.Rehydrate(
      record.Id,
      record.Name,
      record.CreatedBy,
      record.CreatedAtUtc,
      record.Lifecycle,
      auditTrail);
  }

  public static ProjectRecord ToRecord(Project project)
  {
    return new ProjectRecord
    {
      Id = project.Id,
      Name = project.Name,
      CreatedBy = project.CreatedBy,
      CreatedAtUtc = project.CreatedAtUtc,
      Lifecycle = project.Lifecycle,
    };
  }

  public static InspectionSession ToDomain(
    InspectionSessionRecord record,
    IReadOnlyCollection<AuditEvent> auditTrail)
  {
    return InspectionSession.Rehydrate(
      record.Id,
      record.ProjectId,
      record.Name,
      record.CreatedBy,
      record.CreatedAtUtc,
      record.Lifecycle,
      record.StartedAtUtc,
      record.CompletedAtUtc,
      record.FieldNotes.Select(ToDomain).ToArray(),
      record.EvidenceAttachments.Select(ToDomain).ToArray(),
      auditTrail);
  }

  public static InspectionSessionRecord ToRecord(InspectionSession inspectionSession)
  {
    var record = new InspectionSessionRecord
    {
      Id = inspectionSession.Id,
      ProjectId = inspectionSession.ProjectId,
      Name = inspectionSession.Name,
      CreatedBy = inspectionSession.CreatedBy,
      CreatedAtUtc = inspectionSession.CreatedAtUtc,
      Lifecycle = inspectionSession.Lifecycle,
      StartedAtUtc = inspectionSession.StartedAtUtc,
      CompletedAtUtc = inspectionSession.CompletedAtUtc,
    };

    record.FieldNotes.AddRange(inspectionSession.FieldNotes.Select(ToRecord));
    record.EvidenceAttachments.AddRange(inspectionSession.EvidenceAttachments.Select(ToRecord));
    return record;
  }

  public static FieldNote ToDomain(FieldNoteRecord record)
  {
    return new FieldNote(
      record.Id,
      record.InspectionSessionId,
      record.CapturedBy,
      record.CapturedAtUtc,
      new FieldNoteRawText(record.RawText));
  }

  public static FieldNoteRecord ToRecord(FieldNote fieldNote)
  {
    return new FieldNoteRecord
    {
      Id = fieldNote.Id,
      InspectionSessionId = fieldNote.InspectionSessionId,
      CapturedBy = fieldNote.CapturedBy,
      CapturedAtUtc = fieldNote.CapturedAtUtc,
      RawText = fieldNote.RawText.Value,
    };
  }

  public static EvidenceAttachment ToDomain(EvidenceAttachmentRecord record)
  {
    var interpretation = string.IsNullOrWhiteSpace(record.InterpretationSummary)
      ? null
      : new EvidenceInterpretation(
        record.InterpretationSummary,
        record.InterpretedBy!,
        record.InterpretedAtUtc!.Value);

    return EvidenceAttachment.Rehydrate(
      record.Id,
      record.InspectionSessionId,
      record.CapturedBy,
      record.CapturedAtUtc,
      new RawEvidenceReference(record.FileName, record.MediaType, record.StorageKey, record.Checksum),
      interpretation);
  }

  public static EvidenceAttachmentRecord ToRecord(EvidenceAttachment evidenceAttachment)
  {
    return new EvidenceAttachmentRecord
    {
      Id = evidenceAttachment.Id,
      InspectionSessionId = evidenceAttachment.InspectionSessionId,
      CapturedBy = evidenceAttachment.CapturedBy,
      CapturedAtUtc = evidenceAttachment.CapturedAtUtc,
      FileName = evidenceAttachment.RawEvidence.FileName,
      MediaType = evidenceAttachment.RawEvidence.MediaType,
      StorageKey = evidenceAttachment.RawEvidence.StorageKey,
      Checksum = evidenceAttachment.RawEvidence.Checksum,
      InterpretationSummary = evidenceAttachment.Interpretation?.Summary,
      InterpretedBy = evidenceAttachment.Interpretation?.InterpretedBy,
      InterpretedAtUtc = evidenceAttachment.Interpretation?.InterpretedAtUtc,
    };
  }

  public static Report ToDomain(ReportRecord record, IReadOnlyCollection<AuditEvent> auditTrail)
  {
    return Report.Rehydrate(
      record.Id,
      record.ProjectId,
      record.InspectionSessionId,
      record.Title,
      record.RevisionNumber,
      record.Sections
        .OrderBy(section => section.Position)
        .Select(section => new ReportDraftSection(section.Heading, section.Content))
        .ToArray(),
      record.FieldNoteSources
        .Select(source => source.FieldNoteId)
        .ToArray(),
      record.EvidenceSources
        .Select(source => source.EvidenceAttachmentId)
        .ToArray(),
      record.CreatedBy,
      record.CreatedAtUtc,
      record.Lifecycle,
      record.ApprovedBy,
      record.ApprovedAtUtc,
      auditTrail);
  }

  public static ReportRecord ToRecord(Report report)
  {
    return new ReportRecord
    {
      Id = report.Id,
      ProjectId = report.ProjectId,
      InspectionSessionId = report.InspectionSessionId,
      Title = report.Title.Value,
      RevisionNumber = report.RevisionNumber,
      CreatedBy = report.CreatedBy,
      CreatedAtUtc = report.CreatedAtUtc,
      Lifecycle = report.Lifecycle,
      ApprovedBy = report.ApprovedBy,
      ApprovedAtUtc = report.ApprovedAtUtc,
    };
  }

  public static SaveTransaction ToDomain(
    SaveTransactionRecord record,
    IReadOnlyCollection<AuditEvent> auditTrail)
  {
    return SaveTransaction.Rehydrate(
      record.Id,
      record.ReportId,
      record.InitiatedBy,
      record.CreatedAtUtc,
      record.State,
      record.FailureReason,
      record.PreparedAtUtc,
      record.PersistedAtUtc,
      record.CompletedAtUtc,
      auditTrail);
  }

  public static SaveTransactionRecord ToRecord(SaveTransaction saveTransaction)
  {
    return new SaveTransactionRecord
    {
      Id = saveTransaction.Id,
      ReportId = saveTransaction.ReportId,
      InitiatedBy = saveTransaction.InitiatedBy,
      CreatedAtUtc = saveTransaction.CreatedAtUtc,
      State = saveTransaction.State,
      FailureReason = saveTransaction.FailureReason,
      PreparedAtUtc = saveTransaction.PreparedAtUtc,
      PersistedAtUtc = saveTransaction.PersistedAtUtc,
      CompletedAtUtc = saveTransaction.CompletedAtUtc,
    };
  }

  public static AuditEvent ToDomain(AuditEventRecord record)
  {
    return new AuditEvent(
      record.Id,
      record.SubjectType,
      record.SubjectId,
      record.EventType,
      record.Actor,
      record.OccurredAtUtc,
      record.Description);
  }

  public static AuditEventRecord ToRecord(AuditEvent auditEvent)
  {
    return new AuditEventRecord
    {
      Id = auditEvent.Id,
      SubjectType = auditEvent.SubjectType,
      SubjectId = auditEvent.SubjectId,
      EventType = auditEvent.EventType,
      Actor = auditEvent.Actor,
      OccurredAtUtc = auditEvent.OccurredAtUtc,
      Description = auditEvent.Description,
    };
  }

  public static ContextManifest ToDomain(ContextManifestRecord record)
  {
    return new ContextManifest(
      record.Id,
      record.ProjectId,
      record.InspectionSessionId,
      record.ContextPolicyVersion,
      record.Entries
        .OrderBy(entry => entry.Order)
        .Select(entry => new ContextManifestSourceEntry(
          entry.Order,
          entry.ProjectId,
          entry.SourceType,
          entry.SourceId,
          entry.SourceVersion,
          entry.ContentHash,
          entry.AuthorityClassification,
          entry.InclusionReason,
          entry.LimitationNotes,
          entry.IsSuperseded,
          DeserializeStringArray(entry.ConflictCodesJson)))
        .ToArray(),
      DeserializeStringArray(record.IncompleteReasonsJson),
      record.CreatedAtUtc);
  }

  public static ContextManifestRecord ToRecord(ContextManifest contextManifest)
  {
    var record = new ContextManifestRecord
    {
      Id = contextManifest.Id,
      ProjectId = contextManifest.ProjectId,
      InspectionSessionId = contextManifest.InspectionSessionId,
      ContextPolicyVersion = contextManifest.ContextPolicyVersion,
      Status = contextManifest.Status,
      ManifestHash = contextManifest.ManifestHash,
      IncompleteReasonsJson = SerializeStringArray(contextManifest.IncompleteReasons),
      CreatedAtUtc = contextManifest.CreatedAtUtc,
    };

    record.Entries.AddRange(contextManifest.Entries.Select(entry => new ContextManifestSourceEntryRecord
    {
      ContextManifestId = contextManifest.Id,
      Order = entry.Order,
      ProjectId = entry.ProjectId,
      SourceType = entry.SourceType,
      SourceId = entry.SourceId,
      SourceVersion = entry.SourceVersion,
      ContentHash = entry.ContentHash,
      AuthorityClassification = entry.AuthorityClassification,
      InclusionReason = entry.InclusionReason,
      LimitationNotes = entry.LimitationNotes,
      IsSuperseded = entry.IsSuperseded,
      ConflictCodesJson = SerializeStringArray(entry.ConflictCodes),
    }));

    return record;
  }

  public static ModelRun ToDomain(ModelRunRecord record)
  {
    return ModelRun.Rehydrate(
      record.Id,
      record.ProjectId,
      record.InspectionSessionId,
      record.ReportId,
      record.InitiatedBy,
      record.ContextManifestId,
      record.ContextManifestHash,
      record.ProviderId,
      record.ModelName,
      record.ModelDigest,
      record.PromptPackageId,
      record.PromptPackageVersion,
      record.OutputSchemaId,
      record.OutputSchemaVersion,
      record.CorrelationId,
      record.RequestFingerprintHash,
      record.RequestedAtUtc,
      record.State,
      record.FailureClassification,
      record.FailureMessage);
  }

  public static ModelRunRecord ToRecord(ModelRun modelRun)
  {
    return new ModelRunRecord
    {
      Id = modelRun.Id,
      ProjectId = modelRun.ProjectId,
      InspectionSessionId = modelRun.InspectionSessionId,
      ReportId = modelRun.ReportId,
      InitiatedBy = modelRun.InitiatedBy,
      ContextManifestId = modelRun.ContextManifestId,
      ContextManifestHash = modelRun.ContextManifestHash,
      ProviderId = modelRun.ProviderId,
      ModelName = modelRun.ModelName,
      ModelDigest = modelRun.ModelDigest,
      PromptPackageId = modelRun.PromptPackageId,
      PromptPackageVersion = modelRun.PromptPackageVersion,
      OutputSchemaId = modelRun.OutputSchemaId,
      OutputSchemaVersion = modelRun.OutputSchemaVersion,
      CorrelationId = modelRun.CorrelationId,
      RequestFingerprintHash = modelRun.RequestFingerprintHash,
      RequestedAtUtc = modelRun.RequestedAtUtc,
      State = modelRun.State,
      FailureClassification = modelRun.FailureClassification,
      FailureMessage = modelRun.FailureMessage,
    };
  }

  public static ModelRunAttempt ToDomain(ModelRunAttemptRecord record)
  {
    return new ModelRunAttempt(
      record.Id,
      record.ModelRunId,
      record.AttemptNumber,
      record.InputHash,
      record.StartedAtUtc,
      record.CompletedAtUtc,
      record.LatencyMilliseconds,
      record.InputTokenCount,
      record.OutputTokenCount,
      record.RawOutput,
      record.RawOutputHash,
      record.OutcomeClassification,
      record.FailureMessage);
  }

  public static ModelRunAttemptRecord ToRecord(ModelRunAttempt attempt)
  {
    return new ModelRunAttemptRecord
    {
      Id = attempt.Id,
      ModelRunId = attempt.ModelRunId,
      AttemptNumber = attempt.AttemptNumber,
      InputHash = attempt.InputHash,
      StartedAtUtc = attempt.StartedAtUtc,
      CompletedAtUtc = attempt.CompletedAtUtc,
      LatencyMilliseconds = attempt.LatencyMilliseconds,
      InputTokenCount = attempt.InputTokenCount,
      OutputTokenCount = attempt.OutputTokenCount,
      RawOutput = attempt.RawOutput,
      RawOutputHash = attempt.RawOutputHash,
      OutcomeClassification = attempt.OutcomeClassification,
      FailureMessage = attempt.FailureMessage,
    };
  }

  public static AiProposal ToDomain(AiProposalRecord record)
  {
    return AiProposal.Rehydrate(
      record.Id,
      record.ModelRunId,
      record.ProjectId,
      record.InspectionSessionId,
      record.ReportId,
      record.ProviderId,
      record.ModelName,
      record.ModelDigest,
      record.PromptPackageId,
      record.PromptPackageVersion,
      record.OutputSchemaId,
      record.OutputSchemaVersion,
      record.ContextManifestId,
      record.ContextManifestHash,
      record.GeneratedAtUtc,
      record.LatencyMilliseconds,
      record.InputTokenCount,
      record.OutputTokenCount,
      record.Temperature,
      DeserializeStringArray(record.ReferencedSourceIdsJson),
      record.StructuredPayloadJson,
      record.Status,
      record.ConfidenceBand,
      record.AbstentionReason,
      record.ReviewDispositionNotes,
      DeserializeStringArray(record.UncertaintyCodesJson),
      DeserializeStringArray(record.WarningsJson),
      DeserializeStringArray(record.ValidationFailuresJson));
  }

  public static AiProposalRecord ToRecord(AiProposal proposal)
  {
    return new AiProposalRecord
    {
      Id = proposal.Id,
      ModelRunId = proposal.ModelRunId,
      ProjectId = proposal.ProjectId,
      InspectionSessionId = proposal.InspectionSessionId,
      ReportId = proposal.ReportId,
      ProviderId = proposal.ProviderId,
      ModelName = proposal.ModelName,
      ModelDigest = proposal.ModelDigest,
      PromptPackageId = proposal.PromptPackageId,
      PromptPackageVersion = proposal.PromptPackageVersion,
      OutputSchemaId = proposal.OutputSchemaId,
      OutputSchemaVersion = proposal.OutputSchemaVersion,
      ContextManifestId = proposal.ContextManifestId,
      ContextManifestHash = proposal.ContextManifestHash,
      GeneratedAtUtc = proposal.GeneratedAtUtc,
      LatencyMilliseconds = proposal.LatencyMilliseconds,
      InputTokenCount = proposal.InputTokenCount,
      OutputTokenCount = proposal.OutputTokenCount,
      Temperature = proposal.Temperature,
      ReferencedSourceIdsJson = SerializeStringArray(proposal.ReferencedSourceIds),
      StructuredPayloadJson = proposal.StructuredPayloadJson,
      Status = proposal.Status,
      ConfidenceBand = proposal.ConfidenceBand,
      AbstentionReason = proposal.AbstentionReason,
      ReviewDispositionNotes = proposal.ReviewDispositionNotes,
      UncertaintyCodesJson = SerializeStringArray(proposal.UncertaintyCodes),
      WarningsJson = SerializeStringArray(proposal.Warnings),
      ValidationFailuresJson = SerializeStringArray(proposal.ValidationFailures),
    };
  }

  public static KnowledgeDocument ToDomain(
    KnowledgeDocumentRecord record,
    IReadOnlyCollection<AuditEvent> auditTrail)
  {
    return KnowledgeDocument.Rehydrate(
      record.Id,
      record.ProjectId,
      record.DocumentType,
      record.CanonicalTitle,
      record.ExternalReferenceNumber,
      record.DisciplineOrCategory,
      record.CurrentAuthoritativeRevisionId,
      record.Lifecycle,
      record.CreatedBy,
      record.CreatedAtUtc,
      record.Revisions
        .OrderBy(revision => revision.CreatedAtUtc)
        .ThenBy(revision => revision.Id)
        .Select(ToDomain)
        .ToArray(),
      auditTrail);
  }

  public static KnowledgeDocumentRecord ToRecord(KnowledgeDocument knowledgeDocument)
  {
    return new KnowledgeDocumentRecord
    {
      Id = knowledgeDocument.Id,
      ProjectId = knowledgeDocument.ProjectId,
      DocumentType = knowledgeDocument.DocumentType,
      CanonicalTitle = knowledgeDocument.CanonicalTitle,
      ExternalReferenceNumber = knowledgeDocument.ExternalReferenceNumber,
      DisciplineOrCategory = knowledgeDocument.DisciplineOrCategory,
      CurrentAuthoritativeRevisionId = knowledgeDocument.CurrentAuthoritativeRevisionId,
      Lifecycle = knowledgeDocument.Lifecycle,
      CreatedBy = knowledgeDocument.CreatedBy,
      CreatedAtUtc = knowledgeDocument.CreatedAtUtc,
    };
  }

  public static KnowledgeDocumentRevision ToDomain(KnowledgeDocumentRevisionRecord record)
  {
    return KnowledgeDocumentRevision.Rehydrate(
      record.Id,
      record.KnowledgeDocumentId,
      record.KnowledgeSourceId,
      record.RevisionLabel,
      record.EffectiveDate,
      record.ReceivedAtUtc,
      record.SourceAuthority,
      record.VerificationStatus,
      record.ContentHash,
      record.MetadataHash,
      record.SupersedesRevisionId,
      record.SupersededByRevisionId,
      record.SourceSystemReference,
      record.DescriptiveNotes,
      record.CreatedAtUtc,
      record.IngestionStatus,
      record.Lifecycle);
  }

  public static KnowledgeDocumentRevisionRecord ToRecord(KnowledgeDocumentRevision revision)
  {
    return new KnowledgeDocumentRevisionRecord
    {
      Id = revision.Id,
      KnowledgeDocumentId = revision.DocumentId,
      KnowledgeSourceId = revision.KnowledgeSourceId,
      RevisionLabel = revision.RevisionLabel,
      EffectiveDate = revision.EffectiveDate,
      ReceivedAtUtc = revision.ReceivedAtUtc,
      SourceAuthority = revision.SourceAuthority,
      VerificationStatus = revision.VerificationStatus,
      ContentHash = revision.ContentHash,
      MetadataHash = revision.MetadataHash,
      SupersedesRevisionId = revision.SupersedesRevisionId,
      SupersededByRevisionId = revision.SupersededByRevisionId,
      SourceSystemReference = revision.SourceSystemReference,
      DescriptiveNotes = revision.DescriptiveNotes,
      CreatedAtUtc = revision.CreatedAtUtc,
      IngestionStatus = revision.IngestionStatus,
      Lifecycle = revision.Lifecycle,
    };
  }

  public static KnowledgeRelationship ToDomain(
    KnowledgeRelationshipRecord record,
    IReadOnlyCollection<AuditEvent> auditTrail)
  {
    return KnowledgeRelationship.Rehydrate(
      record.Id,
      record.ProjectId,
      ToSubjectReference(
        record.ProjectId,
        record.SourceKind,
        record.SourceDocumentId,
        record.SourceRevisionId),
      ToSubjectReference(
        record.ProjectId,
        record.TargetKind,
        record.TargetDocumentId,
        record.TargetRevisionId),
      record.RelationshipType,
      record.EvidenceOrRationale,
      record.CreatedBy,
      record.CreatedAtUtc,
      record.VerificationStatus,
      auditTrail);
  }

  public static KnowledgeRelationshipRecord ToRecord(KnowledgeRelationship relationship)
  {
    return new KnowledgeRelationshipRecord
    {
      Id = relationship.Id,
      ProjectId = relationship.ProjectId,
      SourceKind = relationship.Source.SubjectKind,
      SourceKey = relationship.Source.ToStableKey(),
      SourceDocumentId = relationship.Source.DocumentId,
      SourceRevisionId = relationship.Source.RevisionId,
      TargetKind = relationship.Target.SubjectKind,
      TargetKey = relationship.Target.ToStableKey(),
      TargetDocumentId = relationship.Target.DocumentId,
      TargetRevisionId = relationship.Target.RevisionId,
      RelationshipType = relationship.RelationshipType,
      EvidenceOrRationale = relationship.EvidenceOrRationale,
      CreatedBy = relationship.CreatedBy,
      CreatedAtUtc = relationship.CreatedAtUtc,
      CreatedAtUtcTicks = relationship.CreatedAtUtc.UtcDateTime.Ticks,
      VerificationStatus = relationship.VerificationStatus,
    };
  }

  public static KnowledgeCitation ToDomain(KnowledgeCitationRecord record)
  {
    return new KnowledgeCitation(
      record.Id,
      record.CitedRevisionId,
      record.LocatorType,
      record.LocatorValue,
      record.RevisionContentHash,
      record.CreatedAtUtc,
      record.QuotedOrSummarizedText);
  }

  public static KnowledgeCitationRecord ToRecord(KnowledgeCitation citation)
  {
    return new KnowledgeCitationRecord
    {
      Id = citation.Id,
      CitedRevisionId = citation.CitedRevisionId,
      LocatorType = citation.LocatorType,
      LocatorValue = citation.LocatorValue,
      RevisionContentHash = citation.RevisionContentHash,
      CreatedAtUtc = citation.CreatedAtUtc,
      QuotedOrSummarizedText = citation.QuotedOrSummarizedText,
    };
  }

  public static StorageObject ToDomain(StorageObjectRecord record)
  {
    return StorageObject.Rehydrate(
      record.Id,
      record.StorageProviderKey,
      record.ImmutableObjectKey,
      record.ContentLength,
      record.ContentHash,
      record.HashAlgorithm,
      record.HashAlgorithmVersion,
      record.CreatedAtUtc,
      record.EncryptionMetadataId,
      record.AvailabilityState);
  }

  public static StorageObjectRecord ToRecord(StorageObject storageObject)
  {
    return new StorageObjectRecord
    {
      Id = storageObject.Id,
      StorageProviderKey = storageObject.StorageProviderKey,
      ImmutableObjectKey = storageObject.ImmutableObjectKey,
      ContentLength = storageObject.ContentLength,
      ContentHash = storageObject.ContentHash,
      HashAlgorithm = storageObject.HashAlgorithm,
      HashAlgorithmVersion = storageObject.HashAlgorithmVersion,
      CreatedAtUtc = storageObject.CreatedAtUtc,
      EncryptionMetadataId = storageObject.EncryptionMetadataId,
      AvailabilityState = storageObject.AvailabilityState,
    };
  }

  public static ImportedDocumentSource ToDomain(
    ImportedDocumentSourceRecord record,
    StorageObjectRecord storageObjectRecord,
    IReadOnlyCollection<AuditEvent> auditTrail)
  {
    return ImportedDocumentSource.Rehydrate(
      record.Id,
      record.ImportSessionId,
      record.ProjectId,
      record.OriginalFileName,
      record.DeclaredMediaType,
      record.DetectedMediaType,
      record.ContentLength,
      record.ContentHash,
      record.HashAlgorithm,
      record.HashAlgorithmVersion,
      ToDomain(storageObjectRecord).ToReference(),
      record.SourceOrigin,
      record.ImportedBy,
      record.ImportedAtUtc,
      record.Status,
      record.ExternalSourceReference,
      auditTrail);
  }

  public static ImportedDocumentSourceRecord ToRecord(ImportedDocumentSource importedDocumentSource)
  {
    return new ImportedDocumentSourceRecord
    {
      Id = importedDocumentSource.Id,
      ImportSessionId = importedDocumentSource.ImportSessionId,
      ProjectId = importedDocumentSource.ProjectId,
      OriginalFileName = importedDocumentSource.OriginalFileName,
      DeclaredMediaType = importedDocumentSource.DeclaredMediaType,
      DetectedMediaType = importedDocumentSource.DetectedMediaType,
      ContentLength = importedDocumentSource.ContentLength,
      ContentHash = importedDocumentSource.ContentHash,
      HashAlgorithm = importedDocumentSource.HashAlgorithm,
      HashAlgorithmVersion = importedDocumentSource.HashAlgorithmVersion,
      StorageObjectId = importedDocumentSource.StorageReference.StorageObjectId,
      SourceOrigin = importedDocumentSource.SourceOrigin,
      ImportedBy = importedDocumentSource.ImportedBy,
      ImportedAtUtc = importedDocumentSource.ImportedAtUtc,
      Status = importedDocumentSource.Status,
      ExternalSourceReference = importedDocumentSource.ExternalSourceReference,
    };
  }

  public static DocumentImportSession ToDomain(
    DocumentImportSessionRecord record,
    IReadOnlyCollection<AuditEvent> auditTrail)
  {
    return DocumentImportSession.Rehydrate(
      record.Id,
      record.ProjectId,
      record.InitiatedBy,
      record.StartedAtUtc,
      record.CompletedAtUtc,
      record.State,
      record.SourceCount,
      record.AcceptedCount,
      record.DuplicateCount,
      record.RejectedCount,
      record.FailureSummary,
      auditTrail);
  }

  public static DocumentImportSessionRecord ToRecord(DocumentImportSession importSession)
  {
    return new DocumentImportSessionRecord
    {
      Id = importSession.Id,
      ProjectId = importSession.ProjectId,
      InitiatedBy = importSession.InitiatedBy,
      StartedAtUtc = importSession.StartedAtUtc,
      CompletedAtUtc = importSession.CompletedAtUtc,
      State = importSession.State,
      SourceCount = importSession.SourceCount,
      AcceptedCount = importSession.AcceptedCount,
      DuplicateCount = importSession.DuplicateCount,
      RejectedCount = importSession.RejectedCount,
      FailureSummary = importSession.FailureSummary,
    };
  }

  public static DocumentProcessingAttempt ToDomain(
    DocumentProcessingAttemptRecord record,
    IReadOnlyCollection<AuditEvent> auditTrail)
  {
    return DocumentProcessingAttempt.Rehydrate(
      record.Id,
      record.ImportedSourceId,
      record.ProjectId,
      record.ProcessorRole,
      record.ProcessorIdentity,
      record.ProcessorVersion,
      record.RequestedAtUtc,
      record.StartedAtUtc,
      record.CompletedAtUtc,
      record.AttemptNumber,
      record.State,
      record.FailureClassification,
      record.FailureDetails,
      record.InputContentHash,
      record.OutputHash,
      auditTrail);
  }

  public static DocumentProcessingAttemptRecord ToRecord(DocumentProcessingAttempt processingAttempt)
  {
    return new DocumentProcessingAttemptRecord
    {
      Id = processingAttempt.Id,
      ImportedSourceId = processingAttempt.ImportedSourceId,
      ProjectId = processingAttempt.ProjectId,
      ProcessorRole = processingAttempt.ProcessorRole,
      ProcessorIdentity = processingAttempt.ProcessorIdentity,
      ProcessorVersion = processingAttempt.ProcessorVersion,
      RequestedAtUtc = processingAttempt.RequestedAtUtc,
      StartedAtUtc = processingAttempt.StartedAtUtc,
      CompletedAtUtc = processingAttempt.CompletedAtUtc,
      AttemptNumber = processingAttempt.AttemptNumber,
      State = processingAttempt.State,
      FailureClassification = processingAttempt.FailureClassification,
      FailureDetails = processingAttempt.FailureDetails,
      InputContentHash = processingAttempt.InputContentHash,
      OutputHash = processingAttempt.OutputHash,
    };
  }

  public static DocumentCandidate ToDomain(
    DocumentCandidateRecord record,
    IReadOnlyCollection<AuditEvent> auditTrail)
  {
    return DocumentCandidate.Rehydrate(
      record.Id,
      record.ProjectId,
      record.ImportedSourceId,
      record.ProcessingAttemptId,
      record.CandidateType,
      record.SchemaId,
      record.SchemaVersion,
      record.PayloadHash,
      record.CanonicalPayload,
      record.SourceContentHash,
      record.SourceLocator,
      record.ConfidenceBand,
      DeserializeStringArray(record.UncertaintyCodesJson),
      record.Status,
      record.CreatedAtUtc,
      record.ReviewedBy,
      record.ReviewedAtUtc,
      record.ReviewNotes,
      auditTrail);
  }

  public static DocumentCandidateRecord ToRecord(DocumentCandidate documentCandidate)
  {
    return new DocumentCandidateRecord
    {
      Id = documentCandidate.Id,
      ProjectId = documentCandidate.ProjectId,
      ImportedSourceId = documentCandidate.ImportedSourceId,
      ProcessingAttemptId = documentCandidate.ProcessingAttemptId,
      CandidateType = documentCandidate.CandidateType,
      SchemaId = documentCandidate.SchemaId,
      SchemaVersion = documentCandidate.SchemaVersion,
      PayloadHash = documentCandidate.PayloadHash,
      CanonicalPayload = documentCandidate.CanonicalPayload,
      SourceContentHash = documentCandidate.SourceContentHash,
      SourceLocator = documentCandidate.SourceLocator,
      ConfidenceBand = documentCandidate.ConfidenceBand,
      UncertaintyCodesJson = SerializeStringArray(documentCandidate.UncertaintyCodes),
      Status = documentCandidate.Status,
      CreatedAtUtc = documentCandidate.CreatedAtUtc,
      ReviewedBy = documentCandidate.ReviewedBy,
      ReviewedAtUtc = documentCandidate.ReviewedAtUtc,
      ReviewNotes = documentCandidate.ReviewNotes,
    };
  }

  private static string SerializeStringArray(IEnumerable<string> values)
  {
    return JsonSerializer.Serialize(
      values.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
      JsonOptions);
  }

  private static string[] DeserializeStringArray(string? json)
  {
    if (string.IsNullOrWhiteSpace(json))
    {
      return [];
    }

    return JsonSerializer.Deserialize<string[]>(json, JsonOptions) ?? [];
  }

  private static KnowledgeSubjectReference ToSubjectReference(
    ProjectId projectId,
    KnowledgeSubjectKind subjectKind,
    KnowledgeDocumentId? documentId,
    KnowledgeDocumentRevisionId? revisionId)
  {
    return subjectKind switch
    {
      KnowledgeSubjectKind.Document when documentId is not null => KnowledgeSubjectReference.ForDocument(projectId, documentId.Value),
      KnowledgeSubjectKind.Revision when revisionId is not null => KnowledgeSubjectReference.ForRevision(projectId, revisionId.Value),
      KnowledgeSubjectKind.Document => throw new InvalidOperationException("Knowledge relationship document subjects must include a document ID."),
      KnowledgeSubjectKind.Revision => throw new InvalidOperationException("Knowledge relationship revision subjects must include a revision ID."),
      _ => throw new InvalidOperationException($"Unsupported knowledge subject kind {subjectKind}."),
    };
  }

  public static ParserRun ToDomain(ParserRunRecord record, IReadOnlyCollection<AuditEvent> auditTrail)
  {
    return ParserRun.Rehydrate(
      record.Id,
      record.ProjectId,
      record.ImportedSourceId,
      record.ParserKey,
      record.ParserVersion,
      record.ParserContractVersion,
      record.ParserContractHash,
      record.SourceContentHash,
      record.SourceHashAlgorithm,
      record.SourceHashAlgorithmVersion,
      record.CreatedBy,
      record.CreatedAtUtc,
      record.State,
      record.ExecutionStatus,
      record.StartedAtUtc,
      record.CompletedAtUtc,
      record.FailureReason,
      auditTrail);
  }

  public static ParserRunRecord ToRecord(ParserRun parserRun)
  {
    return new ParserRunRecord
    {
      Id = parserRun.Id,
      ProjectId = parserRun.ProjectId,
      ImportedSourceId = parserRun.ImportedSourceId,
      ParserKey = parserRun.ParserKey,
      ParserVersion = parserRun.ParserVersion,
      ParserContractVersion = parserRun.ParserContractVersion,
      ParserContractHash = parserRun.ParserContractHash,
      SourceContentHash = parserRun.SourceContentHash,
      SourceHashAlgorithm = parserRun.SourceHashAlgorithm,
      SourceHashAlgorithmVersion = parserRun.SourceHashAlgorithmVersion,
      CreatedBy = parserRun.CreatedBy,
      CreatedAtUtc = parserRun.CreatedAtUtc,
      State = parserRun.State,
      ExecutionStatus = parserRun.ExecutionStatus,
      StartedAtUtc = parserRun.StartedAtUtc,
      CompletedAtUtc = parserRun.CompletedAtUtc,
      FailureReason = parserRun.FailureReason,
    };
  }

  public static FragmentCandidate ToDomain(FragmentCandidateRecord record, IReadOnlyCollection<AuditEvent> auditTrail)
  {
    var locator = new FragmentLocator(record.LocatorType, record.LocatorRawValue);
    return FragmentCandidate.Rehydrate(
      record.Id,
      record.ParserRunId,
      record.ProjectId,
      record.ImportedSourceId,
      record.SourceContentHash,
      locator,
      record.Ordinal,
      record.ContentKind,
      record.ExtractedText,
      record.TextLength,
      record.ConfidenceBand,
      record.IdentityKey,
      record.IdentityKeyHash,
      record.CreatedAtUtc,
      record.ReviewState,
      record.ReviewedBy,
      record.ReviewedAtUtc,
      record.ReviewNotes,
      auditTrail);
  }

  public static FragmentCandidateRecord ToRecord(FragmentCandidate candidate)
  {
    return new FragmentCandidateRecord
    {
      Id = candidate.Id,
      ParserRunId = candidate.ParserRunId,
      ProjectId = candidate.ProjectId,
      ImportedSourceId = candidate.ImportedSourceId,
      SourceContentHash = candidate.SourceContentHash,
      LocatorType = candidate.Locator.LocatorType,
      LocatorRawValue = candidate.Locator.RawValue,
      LocatorNormalizedValue = candidate.Locator.NormalizedValue,
      Ordinal = candidate.Ordinal,
      ContentKind = candidate.ContentKind,
      ExtractedText = candidate.ExtractedText,
      TextLength = candidate.TextLength,
      ConfidenceBand = candidate.ConfidenceBand,
      IdentityKey = candidate.IdentityKey,
      IdentityKeyHash = candidate.IdentityKeyHash,
      CreatedAtUtc = candidate.CreatedAtUtc,
      ReviewState = candidate.ReviewState,
      ReviewedBy = candidate.ReviewedBy,
      ReviewedAtUtc = candidate.ReviewedAtUtc,
      ReviewNotes = candidate.ReviewNotes,
    };
  }

  public static PromotionDiagnostic ToDomain(PromotionDiagnosticRecord record)
  {
    return PromotionDiagnostic.Rehydrate(
      record.Id,
      record.FragmentCandidateId,
      record.ParserRunId,
      record.ProjectId,
      record.PromotedAtUtc,
      record.Status,
      record.FailureReason,
      record.KnowledgeDocumentId,
      record.KnowledgeDocumentRevisionId,
      record.KnowledgeCitationId,
      record.SupersededExistingRevision,
      record.SupersededRevisionId);
  }

  public static PromotionDiagnosticRecord ToRecord(PromotionDiagnostic promotionDiagnostic)
  {
    return new PromotionDiagnosticRecord
    {
      Id = promotionDiagnostic.Id,
      FragmentCandidateId = promotionDiagnostic.FragmentCandidateId,
      ParserRunId = promotionDiagnostic.ParserRunId,
      ProjectId = promotionDiagnostic.ProjectId,
      PromotedAtUtc = promotionDiagnostic.PromotedAtUtc,
      Status = promotionDiagnostic.Status,
      FailureReason = promotionDiagnostic.FailureReason,
      KnowledgeDocumentId = promotionDiagnostic.KnowledgeDocumentId,
      KnowledgeDocumentRevisionId = promotionDiagnostic.KnowledgeDocumentRevisionId,
      KnowledgeCitationId = promotionDiagnostic.KnowledgeCitationId,
      SupersededExistingRevision = promotionDiagnostic.SupersededExistingRevision,
      SupersededRevisionId = promotionDiagnostic.SupersededRevisionId,
    };
  }
}
