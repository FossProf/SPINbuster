namespace SPINbuster.Domain.Tests;

public sealed class ParserEngineTests
{
  private static readonly DateTimeOffset BaseTime = new(2026, 7, 18, 10, 0, 0, TimeSpan.Zero);

  private static ParserRun CreateRun(
    ParserRunId? id = null,
    string parserKey = "test-parser",
    string parserVersion = "1.0.0",
    string parserContractVersion = "1.0.0")
  {
    return new ParserRun(
      id ?? ParserRunId.New(),
      ProjectId.New(),
      ImportedSourceId.New(),
      parserKey,
      parserVersion,
      parserContractVersion,
      "contract-hash",
      "source-hash",
      "SHA-256",
      1,
      "actor",
      BaseTime);
  }

  private static FragmentLocator CreateLocator(
    FragmentLocatorType type = FragmentLocatorType.WholeDocument,
    string rawValue = "whole")
  {
    return new FragmentLocator(type, rawValue);
  }

  private static FragmentCandidate CreateCandidate(
    ParserRunId? parserRunId = null,
    string parserKey = "test-parser",
    string parserContractVersion = "1.0.0",
    int ordinal = 1,
    string extractedText = "sample text")
  {
    return new FragmentCandidate(
      FragmentCandidateId.New(),
      parserRunId ?? ParserRunId.New(),
      ProjectId.New(),
      ImportedSourceId.New(),
      "source-hash",
      CreateLocator(),
      ordinal,
      ContentKind.PlainText,
      extractedText,
      ConfidenceBand.High,
      parserKey,
      parserContractVersion,
      BaseTime);
  }

  // --- ID Tests ---

  [Fact]
  public void ParserRunIdRejectsEmptyGuid()
  {
    Assert.Throws<DomainInvariantException>(() => new ParserRunId(Guid.Empty));
  }

  [Fact]
  public void ParserRunIdNewGeneratesNonEmptyValue()
  {
    var id = ParserRunId.New();
    Assert.NotEqual(Guid.Empty, id.Value);
  }

  [Fact]
  public void ParserRunIdIsDeterministicFromSameInput()
  {
    var guid = Guid.NewGuid();
    var a = new ParserRunId(guid);
    var b = new ParserRunId(guid);

    Assert.Equal(a, b);
  }

  [Fact]
  public void FragmentCandidateIdRejectsEmptyGuid()
  {
    Assert.Throws<DomainInvariantException>(() => new FragmentCandidateId(Guid.Empty));
  }

  [Fact]
  public void FragmentCandidateIdNewGeneratesNonEmptyValue()
  {
    var id = FragmentCandidateId.New();
    Assert.NotEqual(Guid.Empty, id.Value);
  }

  // --- Locator Validation and Normalization ---

  [Fact]
  public void WholeDocumentLocatorNormalizesToEmpty()
  {
    var locator = new FragmentLocator(FragmentLocatorType.WholeDocument, "anything");

    Assert.Equal(FragmentLocatorType.WholeDocument, locator.LocatorType);
    Assert.Equal(string.Empty, locator.NormalizedValue);
  }

  [Fact]
  public void PageLocatorNormalizesNumericValue()
  {
    var locator = new FragmentLocator(FragmentLocatorType.Page, "3");

    Assert.Equal("3", locator.NormalizedValue);
  }

  [Fact]
  public void PageLocatorRejectsNonNumericValue()
  {
    Assert.Throws<DomainInvariantException>(
      () => new FragmentLocator(FragmentLocatorType.Page, "abc"));
  }

  [Fact]
  public void ParagraphLocatorNormalizesPageParagraphFormat()
  {
    var locator = new FragmentLocator(FragmentLocatorType.Paragraph, "3:2");

    Assert.Equal("3:2", locator.NormalizedValue);
  }

  [Fact]
  public void ParagraphLocatorRejectsInvalidFormat()
  {
    Assert.Throws<DomainInvariantException>(
      () => new FragmentLocator(FragmentLocatorType.Paragraph, "not-a-paragraph"));
  }

  [Fact]
  public void LineRangeLocatorNormalizesValidRange()
  {
    var locator = new FragmentLocator(FragmentLocatorType.LineRange, "10-20");

    Assert.Equal("10-20", locator.NormalizedValue);
  }

  [Fact]
  public void LineRangeLocatorRejectsInvalidRange()
  {
    Assert.Throws<DomainInvariantException>(
      () => new FragmentLocator(FragmentLocatorType.LineRange, "20-10"));
  }

  [Fact]
  public void LineRangeLocatorRejectsNonNumericRange()
  {
    Assert.Throws<DomainInvariantException>(
      () => new FragmentLocator(FragmentLocatorType.LineRange, "abc-def"));
  }

  [Fact]
  public void StructuralPathLocatorNormalizesToForwardSlashLowercase()
  {
    var locator = new FragmentLocator(FragmentLocatorType.StructuralPath, @"Section\3.1\Paragraph\2");

    Assert.Equal("section/3.1/paragraph/2", locator.NormalizedValue);
  }

  [Fact]
  public void StructuralPathLocatorTrimsLeadingTrailingSlashes()
  {
    var locator = new FragmentLocator(FragmentLocatorType.StructuralPath, "/section/3/");

    Assert.Equal("section/3", locator.NormalizedValue);
  }

  [Fact]
  public void LocatorRejectsEmptyValue()
  {
    Assert.Throws<DomainInvariantException>(
      () => new FragmentLocator(FragmentLocatorType.Page, ""));
  }

  // --- Candidate Source/Hash/Parser Binding ---

  [Fact]
  public void FragmentCandidateDerivesIdentityKeyFromInputs()
  {
    var sourceId = ImportedSourceId.New();
    var locator = new FragmentLocator(FragmentLocatorType.Page, "3");

    var key = FragmentCandidate.ComputeIdentityKey(sourceId, "pdf-parser", "1.0.0", locator);

    Assert.Contains(sourceId.ToString(), key);
    Assert.Contains("pdf-parser@1.0.0", key);
    Assert.Contains("Page", key);
    Assert.Contains("3", key);
  }

  [Fact]
  public void FragmentCandidateIdentityKeyIsReproducible()
  {
    var sourceId = ImportedSourceId.New();
    var locator = new FragmentLocator(FragmentLocatorType.Page, "3");

    var key1 = FragmentCandidate.ComputeIdentityKey(sourceId, "parser", "1.0.0", locator);
    var key2 = FragmentCandidate.ComputeIdentityKey(sourceId, "parser", "1.0.0", locator);

    Assert.Equal(key1, key2);
  }

  [Fact]
  public void FragmentCandidateDifferentSourceProducesDifferentKey()
  {
    var locator = new FragmentLocator(FragmentLocatorType.Page, "3");

    var key1 = FragmentCandidate.ComputeIdentityKey(ImportedSourceId.New(), "parser", "1.0.0", locator);
    var key2 = FragmentCandidate.ComputeIdentityKey(ImportedSourceId.New(), "parser", "1.0.0", locator);

    Assert.NotEqual(key1, key2);
  }

  [Fact]
  public void FragmentCandidateDifferentParserProducesDifferentKey()
  {
    var sourceId = ImportedSourceId.New();
    var locator = new FragmentLocator(FragmentLocatorType.Page, "3");

    var key1 = FragmentCandidate.ComputeIdentityKey(sourceId, "parser-a", "1.0.0", locator);
    var key2 = FragmentCandidate.ComputeIdentityKey(sourceId, "parser-b", "1.0.0", locator);

    Assert.NotEqual(key1, key2);
  }

  [Fact]
  public void FragmentCandidateDifferentLocatorProducesDifferentKey()
  {
    var sourceId = ImportedSourceId.New();

    var key1 = FragmentCandidate.ComputeIdentityKey(sourceId, "parser", "1.0.0", new FragmentLocator(FragmentLocatorType.Page, "1"));
    var key2 = FragmentCandidate.ComputeIdentityKey(sourceId, "parser", "1.0.0", new FragmentLocator(FragmentLocatorType.Page, "2"));

    Assert.NotEqual(key1, key2);
  }

  [Fact]
  public void FragmentCandidateSourceContentHashMustMatchRun()
  {
    var run = CreateRun();
    var candidate = CreateCandidate(parserRunId: run.Id);

    Assert.Equal(run.SourceContentHash, candidate.SourceContentHash);
  }

  // --- Duplicate Candidate Rejection Within a Run ---

  [Fact]
  public void DuplicateIdentityKeysWithinRunCanBeDetected()
  {
    var sourceId = ImportedSourceId.New();
    var locator = new FragmentLocator(FragmentLocatorType.Page, "3");

    var key1 = FragmentCandidate.ComputeIdentityKey(sourceId, "parser", "1.0.0", locator);
    var key2 = FragmentCandidate.ComputeIdentityKey(sourceId, "parser", "1.0.0", locator);

    Assert.Equal(key1, key2);
  }

  [Fact]
  public void DifferentOrdinalsProduceDifferentCandidates()
  {
    var run = CreateRun();
    var c1 = CreateCandidate(parserRunId: run.Id, ordinal: 1);
    var c2 = CreateCandidate(parserRunId: run.Id, ordinal: 2);

    Assert.NotEqual(c1.Id, c2.Id);
    Assert.Equal(1, c1.Ordinal);
    Assert.Equal(2, c2.Ordinal);
  }

  // --- Lifecycle Transition Success ---

  [Fact]
  public void ParserRunLifecycleHappyPath()
  {
    var run = CreateRun();

    run.Start(BaseTime.AddHours(1));
    Assert.Equal(ParserRunState.Running, run.State);
    Assert.NotNull(run.StartedAtUtc);

    run.Complete(BaseTime.AddHours(2), ParserExecutionStatus.Completed);
    Assert.Equal(ParserRunState.Completed, run.State);
    Assert.NotNull(run.CompletedAtUtc);
  }

  [Fact]
  public void ParserRunFailFromCreated()
  {
    var run = CreateRun();

    run.Fail(BaseTime.AddHours(1), "Setup error");

    Assert.Equal(ParserRunState.Failed, run.State);
    Assert.Equal("Setup error", run.FailureReason);
  }

  [Fact]
  public void ParserRunFailFromRunning()
  {
    var run = CreateRun();
    run.Start(BaseTime.AddHours(1));

    run.Fail(BaseTime.AddHours(2), "Runtime error");

    Assert.Equal(ParserRunState.Failed, run.State);
  }

  [Fact]
  public void ParserRunCancelFromCreated()
  {
    var run = CreateRun();

    run.Cancel(BaseTime.AddHours(1), "User cancelled");

    Assert.Equal(ParserRunState.Cancelled, run.State);
    Assert.Equal("User cancelled", run.FailureReason);
  }

  [Fact]
  public void ParserRunCancelFromRunning()
  {
    var run = CreateRun();
    run.Start(BaseTime.AddHours(1));

    run.Cancel(BaseTime.AddHours(2), "Timeout");

    Assert.Equal(ParserRunState.Cancelled, run.State);
  }

  // --- Lifecycle Transition Failure ---

  [Fact]
  public void ParserRunStartFromRunningThrows()
  {
    var run = CreateRun();
    run.Start(BaseTime.AddHours(1));

    Assert.Throws<LifecycleTransitionException>(() => run.Start(BaseTime.AddHours(2)));
  }

  [Fact]
  public void ParserRunCompleteFromCreatedThrows()
  {
    var run = CreateRun();

    Assert.Throws<LifecycleTransitionException>(() => run.Complete(BaseTime.AddHours(1), ParserExecutionStatus.Completed));
  }

  [Fact]
  public void ParserRunFailFromCompletedThrows()
  {
    var run = CreateRun();
    run.Start(BaseTime.AddHours(1));
    run.Complete(BaseTime.AddHours(2), ParserExecutionStatus.Completed);

    Assert.Throws<LifecycleTransitionException>(() => run.Fail(BaseTime.AddHours(3), "reason"));
  }

  [Fact]
  public void ParserRunCancelFromCompletedThrows()
  {
    var run = CreateRun();
    run.Start(BaseTime.AddHours(1));
    run.Complete(BaseTime.AddHours(2), ParserExecutionStatus.Completed);

    Assert.Throws<LifecycleTransitionException>(() => run.Cancel(BaseTime.AddHours(3), "reason"));
  }

  [Fact]
  public void ParserRunFailFromFailedThrows()
  {
    var run = CreateRun();
    run.Fail(BaseTime.AddHours(1), "first failure");

    Assert.Throws<LifecycleTransitionException>(() => run.Fail(BaseTime.AddHours(2), "second failure"));
  }

  [Fact]
  public void ParserRunCancelFromCancelledThrows()
  {
    var run = CreateRun();
    run.Cancel(BaseTime.AddHours(1), "first cancel");

    Assert.Throws<LifecycleTransitionException>(() => run.Cancel(BaseTime.AddHours(2), "second cancel"));
  }

  // --- Failed Transitions Append No Audit Event ---

  [Fact]
  public void ParserRunFailedTransitionAppendsNoAuditEvent()
  {
    var run = CreateRun();
    run.Start(BaseTime.AddHours(1));
    var countBefore = run.AuditTrail.Count;

    Assert.Throws<LifecycleTransitionException>(() => run.Start(BaseTime.AddHours(2)));

    Assert.Equal(countBefore, run.AuditTrail.Count);
  }

  [Fact]
  public void ParserRunCancelFromTerminalAppendsNoAuditEvent()
  {
    var run = CreateRun();
    run.Start(BaseTime.AddHours(1));
    run.Complete(BaseTime.AddHours(2), ParserExecutionStatus.Completed);
    var countBefore = run.AuditTrail.Count;

    Assert.Throws<LifecycleTransitionException>(() => run.Cancel(BaseTime.AddHours(3), "reason"));

    Assert.Equal(countBefore, run.AuditTrail.Count);
  }

  // --- Terminal Run Immutability ---

  [Fact]
  public void CompletedRunCannotBeStarted()
  {
    var run = CreateRun();
    run.Start(BaseTime.AddHours(1));
    run.Complete(BaseTime.AddHours(2), ParserExecutionStatus.Completed);

    Assert.Throws<LifecycleTransitionException>(() => run.Start(BaseTime.AddHours(3)));
  }

  [Fact]
  public void FailedRunCannotBeCompleted()
  {
    var run = CreateRun();
    run.Start(BaseTime.AddHours(1));
    run.Fail(BaseTime.AddHours(2), "error");

    Assert.Throws<LifecycleTransitionException>(() => run.Complete(BaseTime.AddHours(3), ParserExecutionStatus.Completed));
  }

  // --- Text/Payload Bounds ---

  [Fact]
  public void FragmentCandidateRejectsEmptyExtractedText()
  {
    Assert.Throws<DomainInvariantException>(
      () => CreateCandidate(extractedText: ""));
  }

  [Fact]
  public void FragmentCandidateRejectsExcessiveExtractedText()
  {
    var longText = new string('a', 100_001);

    Assert.Throws<DomainInvariantException>(
      () => CreateCandidate(extractedText: longText));
  }

  [Fact]
  public void FragmentCandidateAcceptsMaximalExtractedText()
  {
    var maxText = new string('a', 100_000);
    var candidate = CreateCandidate(extractedText: maxText);

    Assert.Equal(100_000, candidate.TextLength);
    Assert.Equal(maxText, candidate.ExtractedText);
  }

  [Fact]
  public void FragmentCandidateRejectsZeroOrdinal()
  {
    Assert.Throws<DomainInvariantException>(
      () => CreateCandidate(ordinal: 0));
  }

  [Fact]
  public void FragmentCandidateRejectsNegativeOrdinal()
  {
    Assert.Throws<DomainInvariantException>(
      () => CreateCandidate(ordinal: -1));
  }

  // --- Audit Trail ---

  [Fact]
  public void ParserRunEmitsExplicitEventTypes()
  {
    var run = CreateRun();
    run.Start(BaseTime.AddHours(1));
    run.Complete(BaseTime.AddHours(2), ParserExecutionStatus.Completed);

    var types = run.AuditTrail.Select(e => e.EventType).ToArray();
    Assert.Equal(["ParserRunCreated", "ParserRunStarted", "ParserRunCompleted"], types);
  }

  [Fact]
  public void ParserRunFailEmitsExplicitEventType()
  {
    var run = CreateRun();
    run.Start(BaseTime.AddHours(1));
    run.Fail(BaseTime.AddHours(2), "error");

    var types = run.AuditTrail.Select(e => e.EventType).ToArray();
    Assert.Equal(["ParserRunCreated", "ParserRunStarted", "ParserRunFailed"], types);
  }

  [Fact]
  public void ParserRunCancelEmitsExplicitEventType()
  {
    var run = CreateRun();
    run.Cancel(BaseTime.AddHours(1), "cancelled");

    var types = run.AuditTrail.Select(e => e.EventType).ToArray();
    Assert.Equal(["ParserRunCreated", "ParserRunCancelled"], types);
  }

  [Fact]
  public void FragmentCandidateEmitsGeneratedEvent()
  {
    var candidate = CreateCandidate();

    Assert.Single(candidate.AuditTrail);
    Assert.Equal("FragmentCandidateGenerated", candidate.AuditTrail[0].EventType);
  }

  // --- SubjectType Constants ---

  [Fact]
  public void ParserRunSubjectTypeIsStableConstant()
  {
    var run = CreateRun();

    Assert.Equal("ParserRun", run.AuditTrail[0].SubjectType);
  }

  [Fact]
  public void FragmentCandidateSubjectTypeIsStableConstant()
  {
    var candidate = CreateCandidate();

    Assert.Equal("FragmentCandidate", candidate.AuditTrail[0].SubjectType);
  }

  // --- Source Content Hash Binding ---

  [Fact]
  public void ParserRunBindsToSourceContentHash()
  {
    var run = CreateRun();

    Assert.Equal("source-hash", run.SourceContentHash);
    Assert.Equal("SHA-256", run.SourceHashAlgorithm);
    Assert.Equal(1, run.SourceHashAlgorithmVersion);
  }

  [Fact]
  public void ParserRunRejectsZeroHashAlgorithmVersion()
  {
    Assert.Throws<DomainInvariantException>(
      () => new ParserRun(
        ParserRunId.New(), ProjectId.New(), ImportedSourceId.New(),
        "parser", "1.0.0", "1.0.0", "hash", "src-hash", "SHA-256", 0,
        "actor", BaseTime));
  }

  // --- ContentKind and ConfidenceBand ---

  [Fact]
  public void FragmentCandidatePreservesContentKind()
  {
    var candidate = new FragmentCandidate(
      FragmentCandidateId.New(), ParserRunId.New(), ProjectId.New(),
      ImportedSourceId.New(), "hash", CreateLocator(), 1,
      ContentKind.Table, "table data", ConfidenceBand.Medium,
      "parser", "1.0.0", BaseTime);

    Assert.Equal(ContentKind.Table, candidate.ContentKind);
    Assert.Equal(ConfidenceBand.Medium, candidate.ConfidenceBand);
  }

  // --- Identity Key Hash ---

  [Fact]
  public void FragmentCandidateIdentityKeyHashIsSha256()
  {
    var candidate = CreateCandidate();

    Assert.Equal(64, candidate.IdentityKeyHash.Length);
    Assert.Matches("^[0-9A-F]{64}$", candidate.IdentityKeyHash);
  }

  // --- Append-Only Audit Trail ---

  [Fact]
  public void ParserRunAuditTrailIsAppendOnly()
  {
    var run = CreateRun();
    run.Start(BaseTime.AddHours(1));
    run.Complete(BaseTime.AddHours(2), ParserExecutionStatus.Completed);

    Assert.Equal(3, run.AuditTrail.Count);
    Assert.Equal("ParserRunCompleted", run.AuditTrail[^1].EventType);
  }

  // --- Execution Status Lifecycle Consistency ---

  [Fact]
  public void NewRunHasNullExecutionStatus()
  {
    var run = CreateRun();
    Assert.Null(run.ExecutionStatus);
  }

  [Fact]
  public void RunningRunHasNullExecutionStatus()
  {
    var run = CreateRun();
    run.Start(BaseTime.AddHours(1));
    Assert.Null(run.ExecutionStatus);
  }

  [Fact]
  public void FailSetsExecutionStatusToFailed()
  {
    var run = CreateRun();
    run.Start(BaseTime.AddHours(1));
    run.Fail(BaseTime.AddHours(2), "error");
    Assert.Equal(ParserExecutionStatus.Failed, run.ExecutionStatus);
  }

  [Fact]
  public void CancelSetsExecutionStatusToFailed()
  {
    var run = CreateRun();
    run.Start(BaseTime.AddHours(1));
    run.Cancel(BaseTime.AddHours(2), "cancelled");
    Assert.Equal(ParserExecutionStatus.Failed, run.ExecutionStatus);
  }

  [Fact]
  public void CompleteWithCompletedSetsExecutionStatus()
  {
    var run = CreateRun();
    run.Start(BaseTime.AddHours(1));
    run.Complete(BaseTime.AddHours(2), ParserExecutionStatus.Completed);
    Assert.Equal(ParserExecutionStatus.Completed, run.ExecutionStatus);
  }

  [Fact]
  public void CompleteWithCompletedWithWarningsSetsExecutionStatus()
  {
    var run = CreateRun();
    run.Start(BaseTime.AddHours(1));
    run.Complete(BaseTime.AddHours(2), ParserExecutionStatus.CompletedWithWarnings);
    Assert.Equal(ParserExecutionStatus.CompletedWithWarnings, run.ExecutionStatus);
  }

  [Fact]
  public void CompleteRejectsFailedExecutionStatus()
  {
    var run = CreateRun();
    run.Start(BaseTime.AddHours(1));
    Assert.Throws<DomainInvariantException>(() => run.Complete(BaseTime.AddHours(2), ParserExecutionStatus.Failed));
  }

  // --- Ordinal Uniqueness Detection ---

  [Fact]
  public void FragmentCandidateOrdinalsAreSequential()
  {
    var run = CreateRun();
    var c1 = CreateCandidate(parserRunId: run.Id, ordinal: 1);
    var c2 = CreateCandidate(parserRunId: run.Id, ordinal: 2);
    var c3 = CreateCandidate(parserRunId: run.Id, ordinal: 3);

    Assert.Equal(1, c1.Ordinal);
    Assert.Equal(2, c2.Ordinal);
    Assert.Equal(3, c3.Ordinal);
  }

  // --- Rehydration Validation ---

  [Fact]
  public void RehydrateRestoresPersistedStateWithoutRecomputingIdentity()
  {
    var sourceId = ImportedSourceId.New();
    var locator = new FragmentLocator(FragmentLocatorType.WholeDocument, "*");
    var expectedKey = FragmentCandidate.ComputeIdentityKey(sourceId, "pdf-parser", "2.0.0", locator);
    var expectedHash = ComputeHashHelper(expectedKey);

    var candidate = FragmentCandidate.Rehydrate(
      FragmentCandidateId.New(),
      ParserRunId.New(),
      ProjectId.New(),
      sourceId,
      "source-hash",
      locator,
      1,
      ContentKind.PlainText,
      "parsed content",
      14,
      ConfidenceBand.High,
      expectedKey,
      expectedHash,
      BaseTime,
      FragmentCandidateReviewState.Generated,
      null,
      null,
      null,
      []);

    Assert.Equal(expectedKey, candidate.IdentityKey);
    Assert.Equal(expectedHash, candidate.IdentityKeyHash);
    Assert.Equal(14, candidate.TextLength);
    Assert.Equal("parsed content", candidate.ExtractedText);
  }

  [Fact]
  public void RehydrateRejectsMismatchedTextLength()
  {
    var sourceId = ImportedSourceId.New();
    var locator = new FragmentLocator(FragmentLocatorType.WholeDocument, "*");
    var identityKey = FragmentCandidate.ComputeIdentityKey(sourceId, "parser", "1.0.0", locator);
    var identityKeyHash = ComputeHashHelper(identityKey);

    Assert.Throws<DomainInvariantException>(() =>
      FragmentCandidate.Rehydrate(
        FragmentCandidateId.New(),
        ParserRunId.New(),
        ProjectId.New(),
        sourceId,
        "source-hash",
        locator,
        1,
        ContentKind.PlainText,
        "text",
        99,
        ConfidenceBand.High,
        identityKey,
        identityKeyHash,
        BaseTime,
        FragmentCandidateReviewState.Generated,
        null,
        null,
        null,
        []));
  }

  [Fact]
  public void RehydrateRejectsMismatchedIdentityKeyHash()
  {
    var sourceId = ImportedSourceId.New();
    var locator = new FragmentLocator(FragmentLocatorType.WholeDocument, "*");
    var identityKey = FragmentCandidate.ComputeIdentityKey(sourceId, "parser", "1.0.0", locator);

    Assert.Throws<DomainInvariantException>(() =>
      FragmentCandidate.Rehydrate(
        FragmentCandidateId.New(),
        ParserRunId.New(),
        ProjectId.New(),
        sourceId,
        "source-hash",
        locator,
        1,
        ContentKind.PlainText,
        "text",
        4,
        ConfidenceBand.High,
        identityKey,
        "0000000000000000000000000000000000000000000000000000000000000000",
        BaseTime,
        FragmentCandidateReviewState.Generated,
        null,
        null,
        null,
        []));
  }

  [Fact]
  public void RehydrateRejectsEmptyIdentityKey()
  {
    var sourceId = ImportedSourceId.New();
    var locator = new FragmentLocator(FragmentLocatorType.WholeDocument, "*");

    Assert.Throws<DomainInvariantException>(() =>
      FragmentCandidate.Rehydrate(
        FragmentCandidateId.New(),
        ParserRunId.New(),
        ProjectId.New(),
        sourceId,
        "source-hash",
        locator,
        1,
        ContentKind.PlainText,
        "text",
        4,
        ConfidenceBand.High,
        string.Empty,
        "hash",
        BaseTime,
        FragmentCandidateReviewState.Generated,
        null,
        null,
        null,
        []));
  }

  [Fact]
  public void RehydrateRejectsZeroOrdinal()
  {
    var sourceId = ImportedSourceId.New();
    var locator = new FragmentLocator(FragmentLocatorType.WholeDocument, "*");
    var identityKey = FragmentCandidate.ComputeIdentityKey(sourceId, "parser", "1.0.0", locator);
    var identityKeyHash = ComputeHashHelper(identityKey);

    Assert.Throws<DomainInvariantException>(() =>
      FragmentCandidate.Rehydrate(
        FragmentCandidateId.New(),
        ParserRunId.New(),
        ProjectId.New(),
        sourceId,
        "source-hash",
        locator,
        0,
        ContentKind.PlainText,
        "text",
        4,
        ConfidenceBand.High,
        identityKey,
        identityKeyHash,
        BaseTime,
        FragmentCandidateReviewState.Generated,
        null,
        null,
        null,
        []));
  }

  [Fact]
  public void RehydrateRejectsEmptyExtractedText()
  {
    var sourceId = ImportedSourceId.New();
    var locator = new FragmentLocator(FragmentLocatorType.WholeDocument, "*");
    var identityKey = FragmentCandidate.ComputeIdentityKey(sourceId, "parser", "1.0.0", locator);
    var identityKeyHash = ComputeHashHelper(identityKey);

    Assert.Throws<DomainInvariantException>(() =>
      FragmentCandidate.Rehydrate(
        FragmentCandidateId.New(),
        ParserRunId.New(),
        ProjectId.New(),
        sourceId,
        "source-hash",
        locator,
        1,
        ContentKind.PlainText,
        string.Empty,
        0,
        ConfidenceBand.High,
        identityKey,
        identityKeyHash,
        BaseTime,
        FragmentCandidateReviewState.Generated,
        null,
        null,
        null,
        []));
  }

  [Fact]
  public void IdentityChangesWhenParserKeyChanges()
  {
    var sourceId = ImportedSourceId.New();
    var locator = new FragmentLocator(FragmentLocatorType.Page, "1");

    var key1 = FragmentCandidate.ComputeIdentityKey(sourceId, "parser-a", "1.0.0", locator);
    var key2 = FragmentCandidate.ComputeIdentityKey(sourceId, "parser-b", "1.0.0", locator);

    Assert.NotEqual(key1, key2);
  }

  [Fact]
  public void IdentityChangesWhenContractVersionChanges()
  {
    var sourceId = ImportedSourceId.New();
    var locator = new FragmentLocator(FragmentLocatorType.Page, "1");

    var key1 = FragmentCandidate.ComputeIdentityKey(sourceId, "parser", "1.0.0", locator);
    var key2 = FragmentCandidate.ComputeIdentityKey(sourceId, "parser", "2.0.0", locator);

    Assert.NotEqual(key1, key2);
  }

  [Fact]
  public void ComputeIdentityKeyIsCanonicalAndOwnedByDomain()
  {
    var sourceId = ImportedSourceId.New();
    var locator = new FragmentLocator(FragmentLocatorType.Paragraph, "3:2");

    var key = FragmentCandidate.ComputeIdentityKey(sourceId, "test-parser", "1.0.0", locator);

    Assert.Contains(sourceId.ToString(), key, StringComparison.Ordinal);
    Assert.Contains("test-parser@1.0.0", key, StringComparison.Ordinal);
    Assert.Contains("Paragraph", key, StringComparison.Ordinal);
    Assert.Contains("3:2", key, StringComparison.Ordinal);
  }

  private static string ComputeHashHelper(string value)
  {
    var bytes = System.Text.Encoding.UTF8.GetBytes(value);
    return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
  }

  // --- Review Lifecycle ---

  [Fact]
  public void NewCandidateStartsWithGeneratedReviewState()
  {
    var candidate = CreateCandidate();

    Assert.Equal(FragmentCandidateReviewState.Generated, candidate.ReviewState);
    Assert.Null(candidate.ReviewedBy);
    Assert.Null(candidate.ReviewedAtUtc);
    Assert.Null(candidate.ReviewNotes);
  }

  [Fact]
  public void AcceptTransitionsFromGeneratedToHumanAccepted()
  {
    var candidate = CreateCandidate();

    candidate.Accept("reviewer@example.invalid", BaseTime.AddHours(1), "Looks good.");

    Assert.Equal(FragmentCandidateReviewState.HumanAccepted, candidate.ReviewState);
    Assert.Equal("reviewer@example.invalid", candidate.ReviewedBy);
    Assert.Equal(BaseTime.AddHours(1), candidate.ReviewedAtUtc);
    Assert.Equal("Looks good.", candidate.ReviewNotes);
  }

  [Fact]
  public void RejectTransitionsFromGeneratedToRejected()
  {
    var candidate = CreateCandidate();

    candidate.Reject("reviewer@example.invalid", BaseTime.AddHours(1), "Not relevant.");

    Assert.Equal(FragmentCandidateReviewState.Rejected, candidate.ReviewState);
    Assert.Equal("reviewer@example.invalid", candidate.ReviewedBy);
    Assert.Equal(BaseTime.AddHours(1), candidate.ReviewedAtUtc);
    Assert.Equal("Not relevant.", candidate.ReviewNotes);
  }

  [Fact]
  public void AcceptFromHumanAcceptedThrows()
  {
    var candidate = CreateCandidate();
    candidate.Accept("reviewer@example.invalid", BaseTime.AddHours(1), null);

    Assert.Throws<LifecycleTransitionException>(() =>
      candidate.Accept("other@example.invalid", BaseTime.AddHours(2), null));
  }

  [Fact]
  public void RejectFromHumanAcceptedThrows()
  {
    var candidate = CreateCandidate();
    candidate.Accept("reviewer@example.invalid", BaseTime.AddHours(1), null);

    Assert.Throws<LifecycleTransitionException>(() =>
      candidate.Reject("other@example.invalid", BaseTime.AddHours(2), null));
  }

  [Fact]
  public void AcceptFromRejectedThrows()
  {
    var candidate = CreateCandidate();
    candidate.Reject("reviewer@example.invalid", BaseTime.AddHours(1), null);

    Assert.Throws<LifecycleTransitionException>(() =>
      candidate.Accept("other@example.invalid", BaseTime.AddHours(2), null));
  }

  [Fact]
  public void RejectFromRejectedThrows()
  {
    var candidate = CreateCandidate();
    candidate.Reject("reviewer@example.invalid", BaseTime.AddHours(1), null);

    Assert.Throws<LifecycleTransitionException>(() =>
      candidate.Reject("other@example.invalid", BaseTime.AddHours(2), null));
  }

  [Fact]
  public void AcceptRejectsEmptyActor()
  {
    var candidate = CreateCandidate();

    Assert.Throws<DomainInvariantException>(() =>
      candidate.Accept(string.Empty, BaseTime.AddHours(1), null));
  }

  [Fact]
  public void RejectRejectsEmptyActor()
  {
    var candidate = CreateCandidate();

    Assert.Throws<DomainInvariantException>(() =>
      candidate.Reject(string.Empty, BaseTime.AddHours(1), null));
  }

  [Fact]
  public void AcceptRejectsDefaultTimestamp()
  {
    var candidate = CreateCandidate();

    Assert.Throws<DomainInvariantException>(() =>
      candidate.Accept("reviewer@example.invalid", default, null));
  }

  [Fact]
  public void RejectRejectsDefaultTimestamp()
  {
    var candidate = CreateCandidate();

    Assert.Throws<DomainInvariantException>(() =>
      candidate.Reject("reviewer@example.invalid", default, null));
  }

  [Fact]
  public void AcceptTrimsReviewNotes()
  {
    var candidate = CreateCandidate();

    candidate.Accept("reviewer@example.invalid", BaseTime.AddHours(1), "  trimmed  ");

    Assert.Equal("trimmed", candidate.ReviewNotes);
  }

  [Fact]
  public void AcceptRejectsExcessiveReviewNotes()
  {
    var candidate = CreateCandidate();
    var longNotes = new string('n', 2_001);

    Assert.Throws<DomainInvariantException>(() =>
      candidate.Accept("reviewer@example.invalid", BaseTime.AddHours(1), longNotes));
  }

  [Fact]
  public void AcceptEmitsAuditEvent()
  {
    var candidate = CreateCandidate();

    candidate.Accept("reviewer@example.invalid", BaseTime.AddHours(1), "Accepted.");

    Assert.Equal(2, candidate.AuditTrail.Count);
    Assert.Equal("FragmentCandidateHumanAccepted", candidate.AuditTrail[1].EventType);
    Assert.Equal("reviewer@example.invalid", candidate.AuditTrail[1].Actor);
  }

  [Fact]
  public void RejectEmitsAuditEvent()
  {
    var candidate = CreateCandidate();

    candidate.Reject("reviewer@example.invalid", BaseTime.AddHours(1), "Rejected.");

    Assert.Equal(2, candidate.AuditTrail.Count);
    Assert.Equal("FragmentCandidateRejected", candidate.AuditTrail[1].EventType);
    Assert.Equal("reviewer@example.invalid", candidate.AuditTrail[1].Actor);
  }

  [Fact]
  public void FailedReviewTransitionEmitsNoAuditEvent()
  {
    var candidate = CreateCandidate();
    candidate.Accept("reviewer@example.invalid", BaseTime.AddHours(1), null);
    var countBefore = candidate.AuditTrail.Count;

    Assert.Throws<LifecycleTransitionException>(() =>
      candidate.Reject("other@example.invalid", BaseTime.AddHours(2), null));

    Assert.Equal(countBefore, candidate.AuditTrail.Count);
  }

  [Fact]
  public void ReviewDoesNotMutateIdentityOrProvenance()
  {
    var candidate = CreateCandidate();
    var originalIdentityKey = candidate.IdentityKey;
    var originalIdentityKeyHash = candidate.IdentityKeyHash;
    var originalSourceContentHash = candidate.SourceContentHash;
    var originalLocator = candidate.Locator;
    var originalExtractedText = candidate.ExtractedText;
    var originalParserRunId = candidate.ParserRunId;
    var originalImportedSourceId = candidate.ImportedSourceId;

    candidate.Accept("reviewer@example.invalid", BaseTime.AddHours(1), "Accepted.");

    Assert.Equal(originalIdentityKey, candidate.IdentityKey);
    Assert.Equal(originalIdentityKeyHash, candidate.IdentityKeyHash);
    Assert.Equal(originalSourceContentHash, candidate.SourceContentHash);
    Assert.Equal(originalLocator, candidate.Locator);
    Assert.Equal(originalExtractedText, candidate.ExtractedText);
    Assert.Equal(originalParserRunId, candidate.ParserRunId);
    Assert.Equal(originalImportedSourceId, candidate.ImportedSourceId);
  }

  [Fact]
  public void RehydratePreservesReviewState()
  {
    var sourceId = ImportedSourceId.New();
    var locator = new FragmentLocator(FragmentLocatorType.WholeDocument, "*");
    var identityKey = FragmentCandidate.ComputeIdentityKey(sourceId, "parser", "1.0.0", locator);
    var identityKeyHash = ComputeHashHelper(identityKey);

    var candidate = FragmentCandidate.Rehydrate(
      FragmentCandidateId.New(),
      ParserRunId.New(),
      ProjectId.New(),
      sourceId,
      "source-hash",
      locator,
      1,
      ContentKind.PlainText,
      "text",
      4,
      ConfidenceBand.High,
      identityKey,
      identityKeyHash,
      BaseTime,
      FragmentCandidateReviewState.HumanAccepted,
      "reviewer@example.invalid",
      BaseTime.AddHours(1),
      "Accepted for review.",
      []);

    Assert.Equal(FragmentCandidateReviewState.HumanAccepted, candidate.ReviewState);
    Assert.Equal("reviewer@example.invalid", candidate.ReviewedBy);
    Assert.Equal(BaseTime.AddHours(1), candidate.ReviewedAtUtc);
    Assert.Equal("Accepted for review.", candidate.ReviewNotes);
  }

  [Fact]
  public void RehydrateRejectsAcceptedStateWithoutReviewedBy()
  {
    var sourceId = ImportedSourceId.New();
    var locator = new FragmentLocator(FragmentLocatorType.WholeDocument, "*");
    var identityKey = FragmentCandidate.ComputeIdentityKey(sourceId, "parser", "1.0.0", locator);
    var identityKeyHash = ComputeHashHelper(identityKey);

    Assert.Throws<DomainInvariantException>(() =>
      FragmentCandidate.Rehydrate(
        FragmentCandidateId.New(),
        ParserRunId.New(),
        ProjectId.New(),
        sourceId,
        "source-hash",
        locator,
        1,
        ContentKind.PlainText,
        "text",
        4,
        ConfidenceBand.High,
        identityKey,
        identityKeyHash,
        BaseTime,
        FragmentCandidateReviewState.HumanAccepted,
        null,
        null,
        null,
        []));
  }

  [Fact]
  public void ReviewStateSubjectTypeUsesFragmentCandidateConstant()
  {
    var candidate = CreateCandidate();
    candidate.Accept("reviewer@example.invalid", BaseTime.AddHours(1), null);

    Assert.All(candidate.AuditTrail, e => Assert.Equal("FragmentCandidate", e.SubjectType));
  }
}
