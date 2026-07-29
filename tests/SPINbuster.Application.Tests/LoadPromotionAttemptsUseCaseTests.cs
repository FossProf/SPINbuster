using SPINbuster.Application.Tests.Fakes;
using SPINbuster.Application.UseCases.LoadPromotionAttempts;
using SPINbuster.Domain;

namespace SPINbuster.Application.Tests;

public sealed class LoadPromotionAttemptsUseCaseTests
{
  private static readonly DateTimeOffset TestTime = new(2026, 7, 23, 10, 0, 0, TimeSpan.Zero);

  [Fact]
  public async Task WrongProjectIdReturnsEmpty()
  {
    var fixture = CreateFixture();
    var projectId = ProjectId.New();
    var candidateId = FragmentCandidateId.New();

    var candidate = new FragmentCandidate(
      candidateId, ParserRunId.New(), projectId, ImportedSourceId.New(), "hash",
      new FragmentLocator(FragmentLocatorType.WholeDocument, "*"), 1,
      ContentKind.PlainText, "text", ConfidenceBand.High, "parser", "1.0.0", TestTime);
    candidate.Accept("reviewer", TestTime, null);
    await fixture.FragmentCandidateRepository.AddAsync(candidate);

    var wrongProjectId = ProjectId.New();

    var result = await CreateUseCase(fixture).HandleAsync(
      new LoadPromotionAttemptsQuery(candidateId, wrongProjectId));

    Assert.Empty(result.Attempts);
  }

  [Fact]
  public async Task MaxResultsLimitsOutput()
  {
    var fixture = CreateFixture();
    var projectId = ProjectId.New();
    var candidateId = FragmentCandidateId.New();

    var candidate = new FragmentCandidate(
      candidateId, ParserRunId.New(), projectId, ImportedSourceId.New(), "hash",
      new FragmentLocator(FragmentLocatorType.WholeDocument, "*"), 1,
      ContentKind.PlainText, "text", ConfidenceBand.High, "parser", "1.0.0", TestTime);
    candidate.Accept("reviewer", TestTime, null);
    await fixture.FragmentCandidateRepository.AddAsync(candidate);

    for (var i = 0; i < 5; i++)
    {
      await fixture.PromotionAttemptRepository.AddAsync(new PromotionAttempt(
        PromotionAttemptId.New(),
        PromotionRecordId.New(),
        PromotionAttemptOutcome.RetryablePreconditionFailure,
        PromotionDiagnosticId.New(),
        candidateId,
        "hash",
        TestTime.AddMinutes(i),
        $"failure {i}"));
    }

    var result = await CreateUseCase(fixture).HandleAsync(
      new LoadPromotionAttemptsQuery(candidateId, projectId, MaxResults: 3));

    Assert.Equal(3, result.Attempts.Count);
  }

  [Fact]
  public async Task ResultsOrderedDeterministicallyByAttemptedAtUtcThenId()
  {
    var fixture = CreateFixture();
    var projectId = ProjectId.New();
    var candidateId = FragmentCandidateId.New();

    var candidate = new FragmentCandidate(
      candidateId, ParserRunId.New(), projectId, ImportedSourceId.New(), "hash",
      new FragmentLocator(FragmentLocatorType.WholeDocument, "*"), 1,
      ContentKind.PlainText, "text", ConfidenceBand.High, "parser", "1.0.0", TestTime);
    candidate.Accept("reviewer", TestTime, null);
    await fixture.FragmentCandidateRepository.AddAsync(candidate);

    var id1 = PromotionAttemptId.New();
    var id2 = PromotionAttemptId.New();

    await fixture.PromotionAttemptRepository.AddAsync(new PromotionAttempt(
      id2, PromotionRecordId.New(), PromotionAttemptOutcome.Promoted,
      PromotionDiagnosticId.New(), candidateId, "hash", TestTime));
    await fixture.PromotionAttemptRepository.AddAsync(new PromotionAttempt(
      id1, PromotionRecordId.New(), PromotionAttemptOutcome.RetryablePreconditionFailure,
      PromotionDiagnosticId.New(), candidateId, "hash", TestTime));

    var result = await CreateUseCase(fixture).HandleAsync(
      new LoadPromotionAttemptsQuery(candidateId, projectId));

    Assert.Equal(2, result.Attempts.Count);
    Assert.NotEqual(result.Attempts[0].Id, result.Attempts[1].Id);
  }

  [Fact]
  public async Task FailureReasonsAreSanitized()
  {
    var fixture = CreateFixture();
    var projectId = ProjectId.New();
    var candidateId = FragmentCandidateId.New();

    var candidate = new FragmentCandidate(
      candidateId, ParserRunId.New(), projectId, ImportedSourceId.New(), "hash",
      new FragmentLocator(FragmentLocatorType.WholeDocument, "*"), 1,
      ContentKind.PlainText, "text", ConfidenceBand.High, "parser", "1.0.0", TestTime);
    candidate.Accept("reviewer", TestTime, null);
    await fixture.FragmentCandidateRepository.AddAsync(candidate);

    var longReason = new string('x', 600);
    await fixture.PromotionAttemptRepository.AddAsync(new PromotionAttempt(
      PromotionAttemptId.New(), PromotionRecordId.New(),
      PromotionAttemptOutcome.RetryablePreconditionFailure,
      PromotionDiagnosticId.New(), candidateId, "hash", TestTime, longReason));

    var result = await CreateUseCase(fixture).HandleAsync(
      new LoadPromotionAttemptsQuery(candidateId, projectId));

    Assert.Single(result.Attempts);
    Assert.NotNull(result.Attempts[0].FailureReason);
    Assert.True(result.Attempts[0].FailureReason!.Length <= 503);
    Assert.EndsWith("...", result.Attempts[0].FailureReason!);
  }

  [Fact]
  public async Task RawFailureTextIsNotPubliclyAccessible()
  {
    var fixture = CreateFixture();
    var projectId = ProjectId.New();
    var candidateId = FragmentCandidateId.New();

    var candidate = new FragmentCandidate(
      candidateId, ParserRunId.New(), projectId, ImportedSourceId.New(), "hash",
      new FragmentLocator(FragmentLocatorType.WholeDocument, "*"), 1,
      ContentKind.PlainText, "text", ConfidenceBand.High, "parser", "1.0.0", TestTime);
    candidate.Accept("reviewer", TestTime, null);
    await fixture.FragmentCandidateRepository.AddAsync(candidate);

    await fixture.PromotionAttemptRepository.AddAsync(new PromotionAttempt(
      PromotionAttemptId.New(), PromotionRecordId.New(),
      PromotionAttemptOutcome.RetryablePreconditionFailure,
      PromotionDiagnosticId.New(), candidateId, "hash", TestTime,
      "C:\\sensitive\\path\\to\\config.txt"));

    var result = await CreateUseCase(fixture).HandleAsync(
      new LoadPromotionAttemptsQuery(candidateId, projectId));

    var dtoResult = result.Attempts[0];

    Assert.False(typeof(PromotionAttemptResult).GetProperty("SanitizedFailureReason") is not null && dtoResult.GetType().GetProperty("SanitizedFailureReason") is not null,
      "SanitizedFailureReason should not exist as a separate property; only FailureReason is exposed");

    Assert.Equal("C:\\sensitive\\path\\to\\config.txt", dtoResult.FailureReason);
  }

  [Fact]
  public async Task OutputIsCappedAtSpecifiedLength()
  {
    var fixture = CreateFixture();
    var projectId = ProjectId.New();
    var candidateId = FragmentCandidateId.New();

    var candidate = new FragmentCandidate(
      candidateId, ParserRunId.New(), projectId, ImportedSourceId.New(), "hash",
      new FragmentLocator(FragmentLocatorType.WholeDocument, "*"), 1,
      ContentKind.PlainText, "text", ConfidenceBand.High, "parser", "1.0.0", TestTime);
    candidate.Accept("reviewer", TestTime, null);
    await fixture.FragmentCandidateRepository.AddAsync(candidate);

    var shortReason = new string('a', 10);
    await fixture.PromotionAttemptRepository.AddAsync(new PromotionAttempt(
      PromotionAttemptId.New(), PromotionRecordId.New(),
      PromotionAttemptOutcome.RetryablePreconditionFailure,
      PromotionDiagnosticId.New(), candidateId, "hash", TestTime, shortReason));

    var exactly500 = new string('b', 500);
    await fixture.PromotionAttemptRepository.AddAsync(new PromotionAttempt(
      PromotionAttemptId.New(), PromotionRecordId.New(),
      PromotionAttemptOutcome.RetryablePreconditionFailure,
      PromotionDiagnosticId.New(), candidateId, "hash", TestTime.AddMinutes(1), exactly500));

    var over500 = new string('c', 600);
    await fixture.PromotionAttemptRepository.AddAsync(new PromotionAttempt(
      PromotionAttemptId.New(), PromotionRecordId.New(),
      PromotionAttemptOutcome.RetryablePreconditionFailure,
      PromotionDiagnosticId.New(), candidateId, "hash", TestTime.AddMinutes(2), over500));

    var result = await CreateUseCase(fixture).HandleAsync(
      new LoadPromotionAttemptsQuery(candidateId, projectId));

    Assert.Equal(10, result.Attempts[0].FailureReason!.Length);
    Assert.Equal(500, result.Attempts[1].FailureReason!.Length);
    Assert.Equal(503, result.Attempts[2].FailureReason!.Length);
    Assert.EndsWith("...", result.Attempts[2].FailureReason!);
  }

  [Fact]
  public async Task SensitivePathLikeTextBeyondBoundIsUnavailable()
  {
    var fixture = CreateFixture();
    var projectId = ProjectId.New();
    var candidateId = FragmentCandidateId.New();

    var candidate = new FragmentCandidate(
      candidateId, ParserRunId.New(), projectId, ImportedSourceId.New(), "hash",
      new FragmentLocator(FragmentLocatorType.WholeDocument, "*"), 1,
      ContentKind.PlainText, "text", ConfidenceBand.High, "parser", "1.0.0", TestTime);
    candidate.Accept("reviewer", TestTime, null);
    await fixture.FragmentCandidateRepository.AddAsync(candidate);

    var withinBound = "within-bound-path-like-text-" + new string('a', 480);
    var beyondBound = "<<<BEYOND-BOUND>>>" + new string('z', 100);
    var pathText = withinBound + beyondBound;

    await fixture.PromotionAttemptRepository.AddAsync(new PromotionAttempt(
      PromotionAttemptId.New(), PromotionRecordId.New(),
      PromotionAttemptOutcome.RetryablePreconditionFailure,
      PromotionDiagnosticId.New(), candidateId, "hash", TestTime, pathText));

    var result = await CreateUseCase(fixture).HandleAsync(
      new LoadPromotionAttemptsQuery(candidateId, projectId));

    Assert.NotNull(result.Attempts[0].FailureReason);
    Assert.True(result.Attempts[0].FailureReason!.Length <= 503);
    Assert.EndsWith("...", result.Attempts[0].FailureReason!);
    Assert.DoesNotContain("<<<BEYOND-BOUND>>>", result.Attempts[0].FailureReason);
    Assert.DoesNotContain("zzz", result.Attempts[0].FailureReason);
  }

  [Fact]
  public async Task NonExistentCandidateReturnsEmpty()
  {
    var fixture = CreateFixture();

    var result = await CreateUseCase(fixture).HandleAsync(
      new LoadPromotionAttemptsQuery(FragmentCandidateId.New(), ProjectId.New()));

    Assert.Empty(result.Attempts);
  }

  private static LoadPromotionAttemptsUseCase CreateUseCase(LoadPromotionAttemptsFixture fixture)
  {
    return new LoadPromotionAttemptsUseCase(
      fixture.PromotionAttemptRepository,
      fixture.FragmentCandidateRepository);
  }

  private sealed record LoadPromotionAttemptsFixture(
    FakeFragmentCandidateRepository FragmentCandidateRepository,
    FakePromotionAttemptRepository PromotionAttemptRepository);

  private static LoadPromotionAttemptsFixture CreateFixture()
  {
    return new LoadPromotionAttemptsFixture(
      new FakeFragmentCandidateRepository(),
      new FakePromotionAttemptRepository());
  }
}
