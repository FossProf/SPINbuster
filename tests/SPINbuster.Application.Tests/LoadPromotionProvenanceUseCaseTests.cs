using SPINbuster.Application;
using SPINbuster.Application.Tests.Fakes;
using SPINbuster.Application.UseCases.LoadPromotionProvenance;
using SPINbuster.Domain;

namespace SPINbuster.Application.Tests;

public sealed class LoadPromotionProvenanceUseCaseTests
{
  private static readonly DateTimeOffset TestTime = new(2026, 7, 24, 10, 0, 0, TimeSpan.Zero);

  [Fact]
  public async Task ReturnsCompleteProvenanceChainByRevisionId()
  {
    var fixture = CreateFixture();
    var provenance = SeedProvenance(fixture);

    var result = await CreateUseCase(fixture).HandleAsync(new LoadPromotionProvenanceQuery
    {
      RevisionId = provenance.PromotedRevisionId
    });

    Assert.Equal(provenance.Id, result.Id);
    Assert.Equal(provenance.ProjectId, result.ProjectId);
    Assert.Equal(provenance.PromotedRevisionId, result.PromotedRevisionId);
    Assert.Equal(provenance.DiagnosticId, result.DiagnosticId);
    Assert.Equal(provenance.FragmentCandidateId, result.FragmentCandidateId);
    Assert.Equal(provenance.FragmentSourceContentHash, result.FragmentSourceContentHash);
    Assert.Equal(provenance.ReviewState, result.ReviewState);
    Assert.Equal(provenance.ReviewedBy, result.ReviewedBy);
    Assert.Equal(provenance.ReviewedAtUtc, result.ReviewedAtUtc);
    Assert.Equal(provenance.ParserRunId, result.ParserRunId);
    Assert.Equal(provenance.ParserKey, result.ParserKey);
    Assert.Equal(provenance.ParserVersion, result.ParserVersion);
    Assert.Equal(provenance.ParserContractVersion, result.ParserContractVersion);
    Assert.Equal(provenance.ParserContractHash, result.ParserContractHash);
    Assert.Equal(provenance.ImportedSourceId, result.ImportedSourceId);
    Assert.Equal(provenance.ImportedSourceContentHash, result.ImportedSourceContentHash);
    Assert.Equal(provenance.PromotionIdentityHash, result.PromotionIdentityHash);
    Assert.Equal(provenance.PromotionAttemptId, result.PromotionAttemptId);
    Assert.Equal(provenance.PromotedBy, result.PromotedBy);
    Assert.Equal(provenance.PromotedAtUtc, result.PromotedAtUtc);
  }

  [Fact]
  public async Task ReturnsCompleteProvenanceChainByFragmentCandidateId()
  {
    var fixture = CreateFixture();
    var provenance = SeedProvenance(fixture);

    var result = await CreateUseCase(fixture).HandleAsync(new LoadPromotionProvenanceQuery
    {
      FragmentCandidateId = provenance.FragmentCandidateId
    });

    Assert.Equal(provenance.Id, result.Id);
    Assert.Equal(provenance.ProjectId, result.ProjectId);
    Assert.Equal(provenance.PromotedRevisionId, result.PromotedRevisionId);
    Assert.Equal(provenance.DiagnosticId, result.DiagnosticId);
    Assert.Equal(provenance.FragmentCandidateId, result.FragmentCandidateId);
    Assert.Equal(provenance.PromotionIdentityHash, result.PromotionIdentityHash);
    Assert.Equal(provenance.PromotedBy, result.PromotedBy);
    Assert.Equal(provenance.PromotedAtUtc, result.PromotedAtUtc);
  }

  [Fact]
  public async Task MissingProvenanceReturnsNotFound()
  {
    var fixture = CreateFixture();

    await Assert.ThrowsAsync<ApplicationEntityNotFoundException>(async () =>
      await CreateUseCase(fixture).HandleAsync(new LoadPromotionProvenanceQuery
      {
        RevisionId = KnowledgeDocumentRevisionId.New()
      }));
  }

  [Fact]
  public async Task NeitherRevisionIdNorFragmentCandidateIdReturnsNotFound()
  {
    var fixture = CreateFixture();

    await Assert.ThrowsAsync<ApplicationEntityNotFoundException>(async () =>
      await CreateUseCase(fixture).HandleAsync(new LoadPromotionProvenanceQuery()));
  }

  [Fact]
  public async Task ResultDoesNotContainPhysicalPathsOrRawSourceBytes()
  {
    var fixture = CreateFixture();
    var provenance = SeedProvenance(fixture);

    var result = await CreateUseCase(fixture).HandleAsync(new LoadPromotionProvenanceQuery
    {
      RevisionId = provenance.PromotedRevisionId
    });

    var serialized = result.ToString();
    Assert.DoesNotContain("\\", serialized);
    Assert.DoesNotContain("C:\\", serialized);
  }

  [Fact]
  public async Task IdempotentReplayReturnsSameProvenance()
  {
    var fixture = CreateFixture();
    var provenance = SeedProvenance(fixture);

    var first = await CreateUseCase(fixture).HandleAsync(new LoadPromotionProvenanceQuery
    {
      RevisionId = provenance.PromotedRevisionId
    });

    var second = await CreateUseCase(fixture).HandleAsync(new LoadPromotionProvenanceQuery
    {
      FragmentCandidateId = provenance.FragmentCandidateId
    });

    Assert.Equal(first.Id, second.Id);
    Assert.Equal(first.PromotedRevisionId, second.PromotedRevisionId);
    Assert.Equal(first.PromotedAtUtc, second.PromotedAtUtc);
  }

  private static PromotionProvenance SeedProvenance(PromotionFixture fixture)
  {
    var projectId = ProjectId.New();
    var revisionId = KnowledgeDocumentRevisionId.New();
    var diagnosticId = PromotionDiagnosticId.New();
    var candidateId = FragmentCandidateId.New();
    var parserRunId = ParserRunId.New();
    var importedSourceId = ImportedSourceId.New();
    var attemptId = PromotionAttemptId.New();

    var provenance = new PromotionProvenance(
      PromotionProvenanceId.New(),
      projectId,
      revisionId,
      diagnosticId,
      candidateId,
      "source-content-hash",
      FragmentCandidateReviewState.HumanAccepted,
      "reviewer@example.invalid",
      TestTime,
      parserRunId,
      "test-parser",
      "1.0.0",
      "1.0.0",
      "contract-hash",
      importedSourceId,
      "imported-source-hash",
      "identity-hash",
      attemptId,
      "promoter@example.invalid",
      TestTime);

    fixture.PromotionProvenanceRepository.AddAsync(provenance).GetAwaiter().GetResult();
    return provenance;
  }

  private static LoadPromotionProvenanceUseCase CreateUseCase(PromotionFixture fixture)
  {
    return new LoadPromotionProvenanceUseCase(fixture.PromotionProvenanceRepository);
  }

  private static PromotionFixture CreateFixture()
  {
    return new PromotionFixture(
      new FakePromotionProvenanceRepository());
  }

  private sealed record PromotionFixture(
    FakePromotionProvenanceRepository PromotionProvenanceRepository);
}
