namespace SPINbuster.Domain.Tests;

public sealed class AuditEventTests
{
  private static readonly DateTimeOffset BaseTime = new(2026, 7, 15, 9, 0, 0, TimeSpan.Zero);

  [Fact]
  public void AuditEventIsImmutableByConstruction()
  {
    var occurredAtUtc = new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero);
    var auditEvent = new AuditEvent(
      AuditEventId.New(),
      nameof(Project),
      ProjectId.New().ToString(),
      "ProjectCreated",
      "owner@example.invalid",
      occurredAtUtc,
      "Created.");

    Assert.Equal(nameof(Project), auditEvent.SubjectType);
    Assert.Equal("ProjectCreated", auditEvent.EventType);
    Assert.Equal(occurredAtUtc, auditEvent.OccurredAtUtc);
  }

  [Fact]
  public void AuditTrailIsAppendOnlyForAuditableEntities()
  {
    var project = new Project(
      ProjectId.New(),
      "Project Falcon",
      "owner@example.invalid",
      new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero));

    project.Activate("owner@example.invalid", new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero));
    project.Complete("owner@example.invalid", new DateTimeOffset(2026, 7, 15, 11, 0, 0, TimeSpan.Zero));

    Assert.Equal(3, project.AuditTrail.Count);
    Assert.Equal("ProjectCompleted", project.AuditTrail[^1].EventType);
  }

  [Fact]
  public void ProjectEmitsExplicitEventTypes()
  {
    var project = new Project(ProjectId.New(), "Project Falcon", "actor", BaseTime);

    project.Activate("actor", BaseTime.AddHours(1));
    project.Complete("actor", BaseTime.AddHours(2));

    var types = project.AuditTrail.Select(e => e.EventType).ToArray();
    Assert.Equal(["ProjectCreated", "ProjectActivated", "ProjectCompleted"], types);
  }

  [Fact]
  public void InspectionSessionEmitsExplicitEventTypes()
  {
    var session = new InspectionSession(
      InspectionSessionId.New(), ProjectId.New(), "Field Walk", "actor", BaseTime);

    session.Start("actor", BaseTime.AddHours(1));
    session.Complete("actor", BaseTime.AddHours(2));

    var types = session.AuditTrail.Select(e => e.EventType).ToArray();
    Assert.Equal(
      ["InspectionSessionCreated", "InspectionSessionStarted", "InspectionSessionCompleted"],
      types);
  }

  [Fact]
  public void ReportEmitsExplicitEventTypes()
  {
    var fieldNoteId = FieldNoteId.New();
    var report = new Report(
      ReportId.New(),
      ProjectId.New(),
      InspectionSessionId.New(),
      new ReportTitle("Draft Report"),
      [new ReportDraftSection("Summary", "Initial content")],
      [fieldNoteId],
      [],
      "actor",
      BaseTime);

    report.SubmitForReview("actor", BaseTime.AddHours(1));
    report.Approve("actor", BaseTime.AddHours(2));

    var types = report.AuditTrail.Select(e => e.EventType).ToArray();
    Assert.Equal(
      ["ReportCreated", "ReportSubmittedForReview", "ReportApproved"],
      types);
  }

  [Fact]
  public void SaveTransactionEmitsExplicitEventTypes()
  {
    var tx = new SaveTransaction(SaveTransactionId.New(), ReportId.New(), "actor", BaseTime);

    tx.Prepare("actor", BaseTime.AddHours(1));
    tx.Persist("actor", BaseTime.AddHours(2));
    tx.Commit("actor", BaseTime.AddHours(3));

    var types = tx.AuditTrail.Select(e => e.EventType).ToArray();
    Assert.Equal(
      ["SaveTransactionCreated", "SaveTransactionPrepared", "SaveTransactionPersisted", "SaveTransactionCommitted"],
      types);
  }

  [Fact]
  public void DocumentImportSessionEmitsExplicitEventTypes()
  {
    var session = new DocumentImportSession(
      DocumentImportSessionId.New(), ProjectId.New(), "actor", BaseTime);

    session.BeginValidation("actor", BaseTime.AddHours(1));
    session.BeginImporting("actor", BaseTime.AddHours(2));
    session.RecordAcceptedSource(ImportedSourceId.New(), "actor", BaseTime.AddHours(3));
    session.Complete("actor", BaseTime.AddHours(4));

    var types = session.AuditTrail.Select(e => e.EventType).ToArray();
    Assert.Equal(
      [
        "DocumentImportSessionStarted",
        "DocumentImportValidationStarted",
        "DocumentImportExecutionStarted",
        "ImportedDocumentSourceAccepted",
        "DocumentImportSessionCompleted",
      ],
      types);
  }

  [Fact]
  public void DocumentProcessingAttemptEmitsExplicitEventTypes()
  {
    var attempt = new DocumentProcessingAttempt(
      DocumentProcessingAttemptId.New(),
      ImportedSourceId.New(),
      ProjectId.New(),
      "parser",
      "parser@example.invalid",
      "1.0.0",
      BaseTime,
      1,
      "hash-abc");

    attempt.Start(BaseTime.AddHours(1));
    attempt.MarkOutputReceived(BaseTime.AddHours(2), "output-hash");
    attempt.BeginValidation(BaseTime.AddHours(3));
    attempt.Complete(BaseTime.AddHours(4));

    var types = attempt.AuditTrail.Select(e => e.EventType).ToArray();
    Assert.Equal(
      [
        "DocumentProcessingRequested",
        "DocumentProcessingStarted",
        "DocumentProcessingOutputReceived",
        "DocumentProcessingValidationStarted",
        "DocumentProcessingCompleted",
      ],
      types);
  }

  [Fact]
  public void DocumentCandidateEmitsExplicitEventTypes()
  {
    var candidate = new DocumentCandidate(
      DocumentCandidateId.New(),
      ProjectId.New(),
      ImportedSourceId.New(),
      DocumentProcessingAttemptId.New(),
      DocumentCandidateType.MetadataCandidate,
      "meta-schema",
      "1.0",
      """{"key":"value"}""",
      "src-hash",
      null,
      ConfidenceBand.High,
      [],
      BaseTime);

    candidate.MarkValidated(BaseTime.AddHours(1));
    candidate.MarkReadyForReview(BaseTime.AddHours(2));
    candidate.Accept("reviewer", BaseTime.AddHours(3), null);

    var types = candidate.AuditTrail.Select(e => e.EventType).ToArray();
    Assert.Equal(
      [
        "DocumentCandidateGenerated",
        "DocumentCandidateValidated",
        "DocumentCandidateReadyForReview",
        "DocumentCandidateHumanAccepted",
      ],
      types);
  }

  [Fact]
  public void KnowledgeDocumentEmitsExplicitEventTypes()
  {
    var doc = new KnowledgeDocument(
      KnowledgeDocumentId.New(),
      ProjectId.New(),
      KnowledgeDocumentType.Specification,
      "Structural Specification",
      null,
      null,
      "actor",
      BaseTime);

    doc.Archive("actor", BaseTime.AddHours(1));
    doc.Restore("actor", BaseTime.AddHours(2));

    var types = doc.AuditTrail.Select(e => e.EventType).ToArray();
    Assert.Equal(
      [
        "KnowledgeDocumentRegistered",
        "KnowledgeDocumentArchived",
        "KnowledgeDocumentRestored",
      ],
      types);
  }

  [Fact]
  public void KnowledgeRelationshipEmitsExplicitEventTypes()
  {
    var projectId = ProjectId.New();
    var source = KnowledgeSubjectReference.ForDocument(projectId, KnowledgeDocumentId.New());
    var target = KnowledgeSubjectReference.ForDocument(projectId, KnowledgeDocumentId.New());

    var relationship = new KnowledgeRelationship(
      KnowledgeRelationshipId.New(),
      projectId,
      source,
      target,
      KnowledgeRelationshipType.References,
      "Reference rationale",
      "actor",
      BaseTime);

    var types = relationship.AuditTrail.Select(e => e.EventType).ToArray();
    Assert.Equal(["KnowledgeRelationshipCreated"], types);
  }

  [Fact]
  public void KnowledgeRelationshipContradictsEmitsAdditionalEvent()
  {
    var projectId = ProjectId.New();
    var source = KnowledgeSubjectReference.ForDocument(projectId, KnowledgeDocumentId.New());
    var target = KnowledgeSubjectReference.ForDocument(projectId, KnowledgeDocumentId.New());

    var relationship = new KnowledgeRelationship(
      KnowledgeRelationshipId.New(),
      projectId,
      source,
      target,
      KnowledgeRelationshipType.Contradicts,
      "Contradiction rationale",
      "actor",
      BaseTime);

    var types = relationship.AuditTrail.Select(e => e.EventType).ToArray();
    Assert.Equal(["KnowledgeRelationshipCreated", "KnowledgeContradictionDetected"], types);
  }

  [Fact]
  public void FailedLifecycleTransitionEmitsNoAuditEvent()
  {
    var project = new Project(ProjectId.New(), "Project", "actor", BaseTime);

    var ex = Assert.Throws<LifecycleTransitionException>(
      () => project.Complete("actor", BaseTime.AddHours(1)));

    Assert.Single(project.AuditTrail);
    Assert.Equal("ProjectCreated", project.AuditTrail[0].EventType);
  }

  [Fact]
  public void InspectionSessionCancelledAfterCompletionEmitsNoAuditEvent()
  {
    var session = new InspectionSession(
      InspectionSessionId.New(), ProjectId.New(), "Walk", "actor", BaseTime);
    session.Start("actor", BaseTime.AddHours(1));
    session.Complete("actor", BaseTime.AddHours(2));
    var countBefore = session.AuditTrail.Count;

    Assert.Throws<LifecycleTransitionException>(
      () => session.Cancel("actor", BaseTime.AddHours(3)));

    Assert.Equal(countBefore, session.AuditTrail.Count);
  }

  [Fact]
  public void ReportReturnToDraftFromDraftEmitsNoAuditEvent()
  {
    var fieldNoteId = FieldNoteId.New();
    var report = new Report(
      ReportId.New(), ProjectId.New(), InspectionSessionId.New(),
      new ReportTitle("Draft"),
      [new ReportDraftSection("Intro", "Content")],
      [fieldNoteId], [], "actor", BaseTime);

    Assert.Throws<LifecycleTransitionException>(
      () => report.ReturnToDraft("actor", BaseTime.AddHours(1)));

    Assert.Single(report.AuditTrail);
  }

  [Fact]
  public void SaveTransactionFailFromCommittedEmitsNoAuditEvent()
  {
    var tx = new SaveTransaction(SaveTransactionId.New(), ReportId.New(), "actor", BaseTime);
    tx.Prepare("actor", BaseTime.AddHours(1));
    tx.Persist("actor", BaseTime.AddHours(2));
    tx.Commit("actor", BaseTime.AddHours(3));
    var countBefore = tx.AuditTrail.Count;

    Assert.Throws<LifecycleTransitionException>(
      () => tx.Fail("actor", BaseTime.AddHours(4), "reason"));

    Assert.Equal(countBefore, tx.AuditTrail.Count);
  }

  [Fact]
  public void DocumentProcessingAttemptFailFromCompletedEmitsNoAuditEvent()
  {
    var attempt = new DocumentProcessingAttempt(
      DocumentProcessingAttemptId.New(), ImportedSourceId.New(), ProjectId.New(),
      "parser", "parser@example.invalid", "1.0.0", BaseTime, 1, "hash");
    attempt.Start(BaseTime.AddHours(1));
    attempt.MarkOutputReceived(BaseTime.AddHours(2), "out-hash");
    attempt.BeginValidation(BaseTime.AddHours(3));
    attempt.Complete(BaseTime.AddHours(4));
    var countBefore = attempt.AuditTrail.Count;

    Assert.Throws<LifecycleTransitionException>(
      () => attempt.Fail(BaseTime.AddHours(5), DocumentProcessingFailureClassification.Timeout, "late"));

    Assert.Equal(countBefore, attempt.AuditTrail.Count);
  }

  [Fact]
  public void DocumentCandidateAcceptFromGeneratedEmitsNoAuditEvent()
  {
    var candidate = new DocumentCandidate(
      DocumentCandidateId.New(), ProjectId.New(), ImportedSourceId.New(),
      DocumentProcessingAttemptId.New(), DocumentCandidateType.MetadataCandidate,
      "schema", "1", "{}", "hash", null, ConfidenceBand.High, [], BaseTime);
    var countBefore = candidate.AuditTrail.Count;

    Assert.Throws<LifecycleTransitionException>(
      () => candidate.Accept("reviewer", BaseTime.AddHours(1), null));

    Assert.Equal(countBefore, candidate.AuditTrail.Count);
  }

  [Fact]
  public void NewProjectRetainsCompleteInitialAuditSlice()
  {
    var projectId = ProjectId.New();
    var project = new Project(projectId, "Falcon", "owner", BaseTime);

    Assert.Single(project.AuditTrail);
    Assert.Equal("ProjectCreated", project.AuditTrail[0].EventType);
    Assert.Equal("owner", project.AuditTrail[0].Actor);
    Assert.Equal(BaseTime, project.AuditTrail[0].OccurredAtUtc);
    Assert.Equal(projectId.ToString(), project.AuditTrail[0].SubjectId);
  }

  [Fact]
  public void NewInspectionSessionRetainsCompleteInitialAuditSlice()
  {
    var sessionId = InspectionSessionId.New();
    var session = new InspectionSession(sessionId, ProjectId.New(), "Walk", "actor", BaseTime);

    Assert.Single(session.AuditTrail);
    Assert.Equal("InspectionSessionCreated", session.AuditTrail[0].EventType);
    Assert.Equal(sessionId.ToString(), session.AuditTrail[0].SubjectId);
  }

  [Fact]
  public void NewReportRetainsCompleteInitialAuditSlice()
  {
    var reportId = ReportId.New();
    var fieldNoteId = FieldNoteId.New();
    var report = new Report(
      reportId, ProjectId.New(), InspectionSessionId.New(),
      new ReportTitle("Draft"), [new ReportDraftSection("X", "Y")],
      [fieldNoteId], [], "actor", BaseTime);

    Assert.Single(report.AuditTrail);
    Assert.Equal("ReportCreated", report.AuditTrail[0].EventType);
    Assert.Equal(reportId.ToString(), report.AuditTrail[0].SubjectId);
  }

  [Fact]
  public void NewSaveTransactionRetainsCompleteInitialAuditSlice()
  {
    var txId = SaveTransactionId.New();
    var tx = new SaveTransaction(txId, ReportId.New(), "actor", BaseTime);

    Assert.Single(tx.AuditTrail);
    Assert.Equal("SaveTransactionCreated", tx.AuditTrail[0].EventType);
    Assert.Equal(txId.ToString(), tx.AuditTrail[0].SubjectId);
  }

  [Fact]
  public void NewDocumentImportSessionRetainsCompleteInitialAuditSlice()
  {
    var sessionId = DocumentImportSessionId.New();
    var session = new DocumentImportSession(sessionId, ProjectId.New(), "actor", BaseTime);

    Assert.Single(session.AuditTrail);
    Assert.Equal("DocumentImportSessionStarted", session.AuditTrail[0].EventType);
    Assert.Equal(sessionId.ToString(), session.AuditTrail[0].SubjectId);
  }

  [Fact]
  public void NewDocumentProcessingAttemptRetainsCompleteInitialAuditSlice()
  {
    var attemptId = DocumentProcessingAttemptId.New();
    var attempt = new DocumentProcessingAttempt(
      attemptId, ImportedSourceId.New(), ProjectId.New(),
      "parser", "p@e.com", "1.0", BaseTime, 1, "hash");

    Assert.Single(attempt.AuditTrail);
    Assert.Equal("DocumentProcessingRequested", attempt.AuditTrail[0].EventType);
    Assert.Equal(attemptId.ToString(), attempt.AuditTrail[0].SubjectId);
  }

  [Fact]
  public void NewDocumentCandidateRetainsCompleteInitialAuditSlice()
  {
    var candidateId = DocumentCandidateId.New();
    var candidate = new DocumentCandidate(
      candidateId, ProjectId.New(), ImportedSourceId.New(),
      DocumentProcessingAttemptId.New(), DocumentCandidateType.MetadataCandidate,
      "schema", "1", "{}", "hash", null, ConfidenceBand.High, [], BaseTime);

    Assert.Single(candidate.AuditTrail);
    Assert.Equal("DocumentCandidateGenerated", candidate.AuditTrail[0].EventType);
    Assert.Equal(candidateId.ToString(), candidate.AuditTrail[0].SubjectId);
  }

  [Fact]
  public void NewKnowledgeDocumentRetainsCompleteInitialAuditSlice()
  {
    var docId = KnowledgeDocumentId.New();
    var doc = new KnowledgeDocument(
      docId, ProjectId.New(), KnowledgeDocumentType.Specification,
      "Spec", null, null, "actor", BaseTime);

    Assert.Single(doc.AuditTrail);
    Assert.Equal("KnowledgeDocumentRegistered", doc.AuditTrail[0].EventType);
    Assert.Equal(docId.ToString(), doc.AuditTrail[0].SubjectId);
  }

  [Fact]
  public void NewKnowledgeRelationshipRetainsCompleteInitialAuditSlice()
  {
    var relId = KnowledgeRelationshipId.New();
    var projectId = ProjectId.New();
    var source = KnowledgeSubjectReference.ForDocument(projectId, KnowledgeDocumentId.New());
    var target = KnowledgeSubjectReference.ForDocument(projectId, KnowledgeDocumentId.New());

    var relationship = new KnowledgeRelationship(
      relId, projectId, source, target,
      KnowledgeRelationshipType.References, "Rationale", "actor", BaseTime);

    Assert.Single(relationship.AuditTrail);
    Assert.Equal("KnowledgeRelationshipCreated", relationship.AuditTrail[0].EventType);
    Assert.Equal(relId.ToString(), relationship.AuditTrail[0].SubjectId);
  }

  [Fact]
  public void StagedAuditIsAtomicWithState()
  {
    var project = new Project(ProjectId.New(), "P", "actor", BaseTime);

    project.Activate("actor", BaseTime.AddHours(1));

    Assert.Equal(ProjectLifecycle.Active, project.Lifecycle);
    Assert.Equal(2, project.AuditTrail.Count);
    Assert.Equal("ProjectActivated", project.AuditTrail[^1].EventType);
  }

  [Fact]
  public void InspectionSessionAuditAndStateAreAtomic()
  {
    var session = new InspectionSession(
      InspectionSessionId.New(), ProjectId.New(), "Walk", "actor", BaseTime);

    session.Start("actor", BaseTime.AddHours(1));

    Assert.Equal(InspectionSessionLifecycle.InProgress, session.Lifecycle);
    Assert.NotNull(session.StartedAtUtc);
    Assert.Equal(2, session.AuditTrail.Count);
  }

  [Fact]
  public void ReportDraftUpdateIncrementsRevisionAndAppendsAuditAtomically()
  {
    var fieldNoteId = FieldNoteId.New();
    var report = new Report(
      ReportId.New(), ProjectId.New(), InspectionSessionId.New(),
      new ReportTitle("Draft"),
      [new ReportDraftSection("S", "C")],
      [fieldNoteId], [], "actor", BaseTime);

    report.UpdateDraft(
      new ReportTitle("Revised"),
      [new ReportDraftSection("S", "Updated")],
      "actor",
      BaseTime.AddHours(1));

    Assert.Equal(2, report.RevisionNumber);
    Assert.Equal(2, report.AuditTrail.Count);
    Assert.Equal("ReportDraftUpdated", report.AuditTrail[^1].EventType);
    Assert.Contains("revision 2", report.AuditTrail[^1].Description);
  }

  [Fact]
  public void AuditEventOrderingMatchesAppendOrder()
  {
    var project = new Project(ProjectId.New(), "P", "actor", BaseTime);

    project.Activate("actor", BaseTime.AddHours(1));
    project.Complete("actor", BaseTime.AddHours(2));
    project.Archive("actor", BaseTime.AddHours(3));

    Assert.Equal("ProjectActivated", project.AuditTrail[1].EventType);
    Assert.Equal("ProjectCompleted", project.AuditTrail[2].EventType);
    Assert.Equal("ProjectArchived", project.AuditTrail[3].EventType);
    Assert.True(project.AuditTrail[1].OccurredAtUtc < project.AuditTrail[2].OccurredAtUtc);
  }

  [Fact]
  public void SaveTransactionOrderingIsPreservedAcrossLifecycle()
  {
    var tx = new SaveTransaction(SaveTransactionId.New(), ReportId.New(), "actor", BaseTime);
    tx.Prepare("actor", BaseTime.AddHours(1));
    tx.Persist("actor", BaseTime.AddHours(2));
    tx.Commit("actor", BaseTime.AddHours(3));

    var types = tx.AuditTrail.Select(e => e.EventType).ToArray();
    Assert.Equal(
      ["SaveTransactionCreated", "SaveTransactionPrepared", "SaveTransactionPersisted", "SaveTransactionCommitted"],
      types);
  }

  [Fact]
  public void DescriptionContainsNoAbsoluteFilePath()
  {
    var session = new InspectionSession(
      InspectionSessionId.New(), ProjectId.New(), "Walk", "actor", BaseTime);

    var pathStrings = new[]
    {
      @"C:\Users\someone\file.txt",
      @"D:\Data\evidence.png",
      @"\\server\share\doc.pdf",
    };

    foreach (var evt in session.AuditTrail)
    {
      foreach (var p in pathStrings)
      {
        Assert.DoesNotContain(p, evt.Description);
      }
    }
  }

  [Fact]
  public void DescriptionContainsNoRawEvidencePayload()
  {
    var projectId = ProjectId.New();
    var docId = KnowledgeDocumentId.New();
    var source = KnowledgeSubjectReference.ForDocument(projectId, docId);
    var target = KnowledgeSubjectReference.ForDocument(projectId, KnowledgeDocumentId.New());

    var relationship = new KnowledgeRelationship(
      KnowledgeRelationshipId.New(),
      projectId,
      source,
      target,
      KnowledgeRelationshipType.References,
      "Reference rationale",
      "actor",
      BaseTime);

    foreach (var evt in relationship.AuditTrail)
    {
      Assert.DoesNotContain("base64", evt.Description, StringComparison.OrdinalIgnoreCase);
      Assert.DoesNotContain("<raw", evt.Description, StringComparison.OrdinalIgnoreCase);
    }
  }

  [Fact]
  public void DescriptionContainsNoSecretsOrKeys()
  {
    var session = new InspectionSession(
      InspectionSessionId.New(), ProjectId.New(), "Walk", "actor", BaseTime);

    foreach (var evt in session.AuditTrail)
    {
      Assert.DoesNotContain("password", evt.Description, StringComparison.OrdinalIgnoreCase);
      Assert.DoesNotContain("secret", evt.Description, StringComparison.OrdinalIgnoreCase);
      Assert.DoesNotContain("api_key", evt.Description, StringComparison.OrdinalIgnoreCase);
    }
  }

  [Fact]
  public void AllSubjectTypeConstantsAreNonEmptyStableStrings()
  {
    var pairs = new (AuditableEntity Entity, string ExpectedSubjectType)[]
    {
      (new Project(ProjectId.New(), "P", "a", BaseTime), "Project"),
      (new InspectionSession(InspectionSessionId.New(), ProjectId.New(), "W", "a", BaseTime), "InspectionSession"),
      (new Report(
        ReportId.New(), ProjectId.New(), InspectionSessionId.New(),
        new ReportTitle("T"), [new ReportDraftSection("S", "C")],
        [FieldNoteId.New()], [], "a", BaseTime), "Report"),
      (new SaveTransaction(SaveTransactionId.New(), ReportId.New(), "a", BaseTime), "SaveTransaction"),
    };

    foreach (var (entity, expectedType) in pairs)
    {
      var evt = entity.AuditTrail.Single();
      Assert.Equal(expectedType, evt.SubjectType);
      Assert.False(string.IsNullOrWhiteSpace(evt.SubjectType));
    }
  }

  [Fact]
  public void ImportedDocumentSourceEmitsExplicitEventTypes()
  {
    var storageRef = new DocumentStorageReference(
      StorageObjectId.New(), "local", "obj-key", 100, "hash", "SHA-256", 1,
      BaseTime, null, StorageAvailabilityState.Available);

    var source = new ImportedDocumentSource(
      ImportedSourceId.New(),
      DocumentImportSessionId.New(),
      ProjectId.New(),
      "spec.pdf",
      "application/pdf",
      "application/pdf",
      100,
      "hash",
      "SHA-256",
      1,
      storageRef,
      ImportedSourceOrigin.LocalFile,
      "actor",
      BaseTime,
      ImportedDocumentSourceStatus.Available,
      null);

    var types = source.AuditTrail.Select(e => e.EventType).ToArray();
    Assert.Equal(["ImportedDocumentSourceRegistered"], types);
  }

  [Fact]
  public void ImportedDocumentSourceMarkUnavailableAppendsAuditEvent()
  {
    var storageRef = new DocumentStorageReference(
      StorageObjectId.New(), "local", "obj", 10, "h", "SHA-256", 1,
      BaseTime, null, StorageAvailabilityState.Available);

    var source = new ImportedDocumentSource(
      ImportedSourceId.New(), DocumentImportSessionId.New(), ProjectId.New(),
      "file.txt", null, null, 10, "h", "SHA-256", 1,
      storageRef, ImportedSourceOrigin.LocalFile, "actor", BaseTime,
      ImportedDocumentSourceStatus.Available, null);

    source.MarkUnavailable("actor", BaseTime.AddHours(1), "Storage lost");

    Assert.Equal(2, source.AuditTrail.Count);
    Assert.Equal("ImportedDocumentSourceUnavailable", source.AuditTrail[^1].EventType);
    Assert.Equal("Storage lost", source.AuditTrail[^1].Description);
  }

  [Fact]
  public void DocumentImportSessionFailAppendsAuditEvent()
  {
    var session = new DocumentImportSession(
      DocumentImportSessionId.New(), ProjectId.New(), "actor", BaseTime);

    session.Fail("actor", BaseTime.AddHours(1), "Validation failed");

    Assert.Equal(2, session.AuditTrail.Count);
    Assert.Equal("DocumentImportSessionFailed", session.AuditTrail[^1].EventType);
  }

  [Fact]
  public void DocumentProcessingAttemptFailAppendsAuditEvent()
  {
    var attempt = new DocumentProcessingAttempt(
      DocumentProcessingAttemptId.New(), ImportedSourceId.New(), ProjectId.New(),
      "parser", "p@e.com", "1.0", BaseTime, 1, "hash");
    attempt.Start(BaseTime.AddHours(1));

    attempt.Fail(
      BaseTime.AddHours(2),
      DocumentProcessingFailureClassification.Timeout,
      "Provider timed out");

    Assert.Equal(3, attempt.AuditTrail.Count);
    Assert.Equal("DocumentProcessingFailed", attempt.AuditTrail[^1].EventType);
  }

  [Fact]
  public void DocumentCandidateSchemaRejectedAppendsAuditEvent()
  {
    var candidate = new DocumentCandidate(
      DocumentCandidateId.New(), ProjectId.New(), ImportedSourceId.New(),
      DocumentProcessingAttemptId.New(), DocumentCandidateType.MetadataCandidate,
      "schema", "1", "{}", "hash", null, ConfidenceBand.High, [], BaseTime);

    candidate.MarkSchemaRejected(BaseTime.AddHours(1), ["missing-field"]);

    Assert.Equal(2, candidate.AuditTrail.Count);
    Assert.Equal("DocumentCandidateSchemaRejected", candidate.AuditTrail[^1].EventType);
  }

  [Fact]
  public void DocumentCandidateRejectAppendsAuditEvent()
  {
    var candidate = new DocumentCandidate(
      DocumentCandidateId.New(), ProjectId.New(), ImportedSourceId.New(),
      DocumentProcessingAttemptId.New(), DocumentCandidateType.MetadataCandidate,
      "schema", "1", "{}", "hash", null, ConfidenceBand.High, [], BaseTime);
    candidate.MarkValidated(BaseTime.AddHours(1));
    candidate.MarkReadyForReview(BaseTime.AddHours(2));

    candidate.Reject("reviewer", BaseTime.AddHours(3), "Not applicable");

    Assert.Equal(4, candidate.AuditTrail.Count);
    Assert.Equal("DocumentCandidateRejected", candidate.AuditTrail[^1].EventType);
  }

  [Fact]
  public void KnowledgeDocumentArchiveAndRestoreAppendEvents()
  {
    var doc = new KnowledgeDocument(
      KnowledgeDocumentId.New(), ProjectId.New(),
      KnowledgeDocumentType.Drawing, "Title", null, null, "actor", BaseTime);

    doc.Archive("actor", BaseTime.AddHours(1));
    doc.Restore("actor", BaseTime.AddHours(2));

    Assert.Equal(3, doc.AuditTrail.Count);
    Assert.Equal("KnowledgeDocumentArchived", doc.AuditTrail[1].EventType);
    Assert.Equal("KnowledgeDocumentRestored", doc.AuditTrail[2].EventType);
  }

  [Fact]
  public void KnowledgeRelationshipContradictsAddsTwoEvents()
  {
    var projectId = ProjectId.New();
    var source = KnowledgeSubjectReference.ForDocument(projectId, KnowledgeDocumentId.New());
    var target = KnowledgeSubjectReference.ForDocument(projectId, KnowledgeDocumentId.New());

    var relationship = new KnowledgeRelationship(
      KnowledgeRelationshipId.New(),
      projectId,
      source,
      target,
      KnowledgeRelationshipType.Contradicts,
      "Contradiction rationale",
      "actor",
      BaseTime);

    Assert.Equal(2, relationship.AuditTrail.Count);
    Assert.Equal("KnowledgeRelationshipCreated", relationship.AuditTrail[0].EventType);
    Assert.Equal("KnowledgeContradictionDetected", relationship.AuditTrail[1].EventType);
  }

  [Fact]
  public void NonContradictsRelationshipAddsSingleEvent()
  {
    var projectId = ProjectId.New();
    var source = KnowledgeSubjectReference.ForDocument(projectId, KnowledgeDocumentId.New());
    var target = KnowledgeSubjectReference.ForDocument(projectId, KnowledgeDocumentId.New());

    var relationship = new KnowledgeRelationship(
      KnowledgeRelationshipId.New(),
      projectId,
      source,
      target,
      KnowledgeRelationshipType.References,
      "Reference rationale",
      "actor",
      BaseTime);

    Assert.Single(relationship.AuditTrail);
  }
}
