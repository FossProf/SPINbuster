using Microsoft.Extensions.Logging.Abstractions;
using SPINbuster.Application.Tests.Fakes;
using SPINbuster.Application.UseCases.LoadPromotionAttempts;
using SPINbuster.Application.UseCases.PromoteFragmentCandidate;
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
    Assert.NotNull(result.Attempts[0].SanitizedFailureReason);
    Assert.True(result.Attempts[0].SanitizedFailureReason!.Length <= 510);
    Assert.EndsWith("...", result.Attempts[0].SanitizedFailureReason!);
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
