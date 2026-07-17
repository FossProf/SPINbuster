using SPINbuster.Domain;

namespace SPINbuster.Domain.Tests;

public sealed class DocumentEngineDomainTests
{
  [Fact]
  public void ImportedDocumentSourceRejectsStorageHashMismatch()
  {
    var storage = new DocumentStorageReference(
      StorageObjectId.New(),
      "provider",
      "object-key",
      10,
      "abc",
      "SHA-256",
      1,
      new DateTimeOffset(2026, 7, 16, 12, 0, 0, TimeSpan.Zero),
      null,
      StorageAvailabilityState.Available);

    var exception = Assert.Throws<DomainInvariantException>(() => new ImportedDocumentSource(
      ImportedSourceId.New(),
      DocumentImportSessionId.New(),
      ProjectId.New(),
      "detail.pdf",
      "application/pdf",
      "application/pdf",
      10,
      "def",
      "SHA-256",
      1,
      storage,
      ImportedSourceOrigin.LocalFile,
      "user@example.invalid",
      new DateTimeOffset(2026, 7, 16, 12, 0, 0, TimeSpan.Zero),
      ImportedDocumentSourceStatus.Available,
      null));

    Assert.Contains("hash", exception.Message, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public void ImportSessionCountsRemainConsistent()
  {
    var session = new DocumentImportSession(
      DocumentImportSessionId.New(),
      ProjectId.New(),
      "user@example.invalid",
      new DateTimeOffset(2026, 7, 16, 12, 0, 0, TimeSpan.Zero));

    session.BeginValidation("user@example.invalid", new DateTimeOffset(2026, 7, 16, 12, 1, 0, TimeSpan.Zero));
    session.BeginImporting("user@example.invalid", new DateTimeOffset(2026, 7, 16, 12, 2, 0, TimeSpan.Zero));
    session.RecordAcceptedSource(ImportedSourceId.New(), "user@example.invalid", new DateTimeOffset(2026, 7, 16, 12, 3, 0, TimeSpan.Zero));
    session.RecordDuplicateSource(ImportedSourceId.New(), "user@example.invalid", new DateTimeOffset(2026, 7, 16, 12, 4, 0, TimeSpan.Zero));
    session.RecordRejectedSource("user@example.invalid", new DateTimeOffset(2026, 7, 16, 12, 5, 0, TimeSpan.Zero), "Unsupported file.");
    session.Complete("user@example.invalid", new DateTimeOffset(2026, 7, 16, 12, 6, 0, TimeSpan.Zero));

    Assert.Equal(3, session.SourceCount);
    Assert.Equal(1, session.AcceptedCount);
    Assert.Equal(1, session.DuplicateCount);
    Assert.Equal(1, session.RejectedCount);
  }

  [Fact]
  public void ImportSessionAllowsRepeatedValidationAndImportingWithinOneBatch()
  {
    var session = new DocumentImportSession(
      DocumentImportSessionId.New(),
      ProjectId.New(),
      "user@example.invalid",
      new DateTimeOffset(2026, 7, 16, 12, 0, 0, TimeSpan.Zero));

    session.BeginValidation("user@example.invalid", new DateTimeOffset(2026, 7, 16, 12, 1, 0, TimeSpan.Zero));
    session.BeginImporting("user@example.invalid", new DateTimeOffset(2026, 7, 16, 12, 2, 0, TimeSpan.Zero));
    session.RecordAcceptedSource(ImportedSourceId.New(), "user@example.invalid", new DateTimeOffset(2026, 7, 16, 12, 3, 0, TimeSpan.Zero));
    session.BeginValidation("user@example.invalid", new DateTimeOffset(2026, 7, 16, 12, 4, 0, TimeSpan.Zero));
    session.BeginImporting("user@example.invalid", new DateTimeOffset(2026, 7, 16, 12, 5, 0, TimeSpan.Zero));
    session.RecordDuplicateSource(ImportedSourceId.New(), "user@example.invalid", new DateTimeOffset(2026, 7, 16, 12, 6, 0, TimeSpan.Zero));

    Assert.Equal(DocumentImportSessionState.Importing, session.State);
    Assert.Equal(2, session.SourceCount);
    Assert.Equal(1, session.AcceptedCount);
    Assert.Equal(1, session.DuplicateCount);
  }

  [Fact]
  public void ProcessingAttemptRejectsTerminalTransitionAfterFailure()
  {
    var attempt = new DocumentProcessingAttempt(
      DocumentProcessingAttemptId.New(),
      ImportedSourceId.New(),
      ProjectId.New(),
      "role",
      "processor",
      "1.0.0",
      new DateTimeOffset(2026, 7, 16, 12, 0, 0, TimeSpan.Zero),
      1,
      "hash");

    attempt.Start(new DateTimeOffset(2026, 7, 16, 12, 1, 0, TimeSpan.Zero));
    attempt.Fail(new DateTimeOffset(2026, 7, 16, 12, 2, 0, TimeSpan.Zero), DocumentProcessingFailureClassification.ProviderUnavailable, "Provider unavailable.");

    Assert.Throws<LifecycleTransitionException>(() => attempt.Complete(new DateTimeOffset(2026, 7, 16, 12, 3, 0, TimeSpan.Zero)));
  }

  [Fact]
  public void HumanAcceptedCandidateCannotReturnToReviewableState()
  {
    var candidate = new DocumentCandidate(
      DocumentCandidateId.New(),
      ProjectId.New(),
      ImportedSourceId.New(),
      DocumentProcessingAttemptId.New(),
      DocumentCandidateType.MetadataCandidate,
      "schema",
      "1.0.0",
      """{"a":1}""",
      "hash",
      null,
      ConfidenceBand.High,
      [],
      new DateTimeOffset(2026, 7, 16, 12, 0, 0, TimeSpan.Zero));

    candidate.MarkValidated(new DateTimeOffset(2026, 7, 16, 12, 1, 0, TimeSpan.Zero));
    candidate.MarkReadyForReview(new DateTimeOffset(2026, 7, 16, 12, 2, 0, TimeSpan.Zero));
    candidate.Accept("reviewer@example.invalid", new DateTimeOffset(2026, 7, 16, 12, 3, 0, TimeSpan.Zero), "Accepted for later promotion review.");

    Assert.Throws<LifecycleTransitionException>(() => candidate.MarkReadyForReview(new DateTimeOffset(2026, 7, 16, 12, 4, 0, TimeSpan.Zero)));
  }
}
