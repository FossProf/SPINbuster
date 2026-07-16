namespace SPINbuster.Domain.Tests;

public sealed class AiSubstrateTests
{
  [Fact]
  public void ContextManifestRejectsCrossProjectSources()
  {
    var projectId = ProjectId.New();

    Assert.Throws<DomainInvariantException>(() => new ContextManifest(
      ContextManifestId.New(),
      projectId,
      InspectionSessionId.New(),
      "policy/1.0",
      [
        new ContextManifestSourceEntry(
          0,
          ProjectId.New(),
          ContextSourceType.FieldNote,
          "source-1",
          "raw-v1",
          "hash-1",
          AuthorityClassification.Authoritative,
          "Included for review.",
          null,
          false,
          [])
      ],
      [],
      Timestamp(0)));
  }

  [Fact]
  public void ContextManifestHashIsStableForEquivalentInput()
  {
    var projectId = ProjectId.New();
    var inspectionSessionId = InspectionSessionId.New();
    var entries = new[]
    {
      new ContextManifestSourceEntry(
        0,
        projectId,
        ContextSourceType.FieldNote,
        "field-note-1",
        "raw-v1",
        "hash-1",
        AuthorityClassification.Authoritative,
        "Included for review.",
        null,
        false,
        []),
    };

    var first = new ContextManifest(
      ContextManifestId.New(),
      projectId,
      inspectionSessionId,
      "policy/1.0",
      entries,
      [],
      Timestamp(0));
    var second = new ContextManifest(
      ContextManifestId.New(),
      projectId,
      inspectionSessionId,
      "policy/1.0",
      entries,
      [],
      Timestamp(1));

    Assert.Equal(first.ManifestHash, second.ManifestHash);
  }

  [Fact]
  public void ContextManifestSupportsIncompleteStatus()
  {
    var projectId = ProjectId.New();
    var manifest = new ContextManifest(
      ContextManifestId.New(),
      projectId,
      InspectionSessionId.New(),
      "policy/1.0",
      [
        new ContextManifestSourceEntry(
          0,
          projectId,
          ContextSourceType.FieldNote,
          "field-note-1",
          "raw-v1",
          "hash-1",
          AuthorityClassification.Authoritative,
          "Included for review.",
          null,
          false,
          [])
      ],
      ["missing-authoritative-source"],
      Timestamp(0));

    Assert.Equal(ContextManifestStatus.Incomplete, manifest.Status);
    Assert.Contains("missing-authoritative-source", manifest.IncompleteReasons);
  }

  [Fact]
  public void ModelRunRejectsInvalidTransition()
  {
    var modelRun = CreateModelRun();

    Assert.Throws<LifecycleTransitionException>(() => modelRun.MarkReadyForHumanReview());
  }

  [Fact]
  public void ModelRunSupportsReviewableLifecycle()
  {
    var modelRun = CreateModelRun();

    modelRun.MarkContextBuilding();
    modelRun.MarkContextValidated();
    modelRun.Queue();
    modelRun.StartRunning();
    modelRun.MarkOutputReceived();
    modelRun.MarkSchemaValidating();
    modelRun.MarkPolicyValidating();
    modelRun.MarkReadyForHumanReview();
    modelRun.MarkReviewCompleted();
    modelRun.Close();

    Assert.Equal(ModelRunState.Closed, modelRun.State);
  }

  [Fact]
  public void ProposalAcceptanceIsAdvisoryOnly()
  {
    var proposal = CreateProposal();

    proposal.MarkReadyForReview(ConfidenceBand.Medium, [], []);
    proposal.Accept("Accepted for later deterministic processing.");

    Assert.Equal(ProposalStatus.HumanAccepted, proposal.Status);
  }

  [Fact]
  public void AiProposalRejectsInvalidTerminalTransition()
  {
    var proposal = CreateProposal();
    proposal.MarkReadyForReview(ConfidenceBand.Medium, [], []);
    proposal.Reject("reviewer rejected the proposal.");

    Assert.Throws<LifecycleTransitionException>(() => proposal.Accept("cannot accept after reject"));
  }

  [Fact]
  public void AiProposalRequiresValidationFailuresForRejectedStates()
  {
    var proposal = CreateProposal();

    Assert.Throws<DomainInvariantException>(() => proposal.MarkSchemaRejected(ConfidenceBand.None, [], [], []));
  }

  [Fact]
  public void AiProposalSupportsAbstentionInvariant()
  {
    var payload = new AiProposalPayload(
      [],
      string.Empty,
      ConfidenceBand.None,
      [],
      [],
      [],
      [],
      [],
      "Insufficient context.");

    Assert.Equal("Insufficient context.", payload.AbstentionReason);
    Assert.Empty(payload.Sections);
  }

  private static ModelRun CreateModelRun()
  {
    return new ModelRun(
      ModelRunId.New(),
      ProjectId.New(),
      InspectionSessionId.New(),
      ReportId.New(),
      "inspector@example.invalid",
      ContextManifestId.New(),
      "manifest-hash",
      "provider-id",
      "model-name",
      "model-digest",
      "prompt-package",
      "0.1.0",
      "schema-id",
      "1.0.0",
      "correlation-id",
      "request-fingerprint",
      Timestamp(0));
  }

  private static AiProposal CreateProposal()
  {
    return new AiProposal(
      ProposalId.New(),
      ModelRunId.New(),
      ProjectId.New(),
      InspectionSessionId.New(),
      ReportId.New(),
      "provider-id",
      "model-name",
      "model-digest",
      "prompt-package",
      "0.1.0",
      "schema-id",
      "1.0.0",
      ContextManifestId.New(),
      "manifest-hash",
      Timestamp(1),
      42,
      12,
      8,
      0.2m,
      ["field-note-1"],
      "{}");
  }

  private static DateTimeOffset Timestamp(int hoursOffset)
  {
    return new DateTimeOffset(2026, 7, 15, 9 + hoursOffset, 0, 0, TimeSpan.Zero);
  }
}
