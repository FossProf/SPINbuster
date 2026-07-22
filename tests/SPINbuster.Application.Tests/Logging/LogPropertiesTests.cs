using SPINbuster.Application.Logging;

namespace SPINbuster.Application.Tests.Logging;

public sealed class LogPropertiesTests
{
  [Fact]
  public void AllPropertyNamesAreNonEmpty()
  {
    var properties = new[]
    {
      LogProperties.ProjectId,
      LogProperties.OperationId,
      LogProperties.ApplicationUserId,
      LogProperties.UseCase,
      LogProperties.AttemptId,
      LogProperties.ProviderKey,
      LogProperties.FailureClassification,
      LogProperties.DurationMs,
      LogProperties.ImportSessionId,
      LogProperties.ImportedSourceId,
      LogProperties.ModelRunId,
      LogProperties.ProposalId,
      LogProperties.ReportId,
      LogProperties.InspectionSessionId,
      LogProperties.CorrelationId,
      LogProperties.ContentHash,
      LogProperties.FileName,
      LogProperties.DeclaredMediaType,
      LogProperties.ProcessorKey,
      LogProperties.CandidateCount,
      LogProperties.FragmentCandidateId,
      LogProperties.ParserRunId,
      LogProperties.ReviewState,
    };

    Assert.All(properties, name => Assert.False(string.IsNullOrWhiteSpace(name)));
  }

  [Fact]
  public void PropertyNamesMatchTheirFieldName()
  {
    Assert.Equal("ProjectId", LogProperties.ProjectId);
    Assert.Equal("OperationId", LogProperties.OperationId);
    Assert.Equal("ApplicationUserId", LogProperties.ApplicationUserId);
    Assert.Equal("UseCase", LogProperties.UseCase);
    Assert.Equal("AttemptId", LogProperties.AttemptId);
    Assert.Equal("ProviderKey", LogProperties.ProviderKey);
    Assert.Equal("FailureClassification", LogProperties.FailureClassification);
    Assert.Equal("DurationMs", LogProperties.DurationMs);
    Assert.Equal("ContentHash", LogProperties.ContentHash);
    Assert.Equal("FileName", LogProperties.FileName);
    Assert.Equal("DeclaredMediaType", LogProperties.DeclaredMediaType);
    Assert.Equal("ProcessorKey", LogProperties.ProcessorKey);
    Assert.Equal("CandidateCount", LogProperties.CandidateCount);
    Assert.Equal("FragmentCandidateId", LogProperties.FragmentCandidateId);
    Assert.Equal("ParserRunId", LogProperties.ParserRunId);
    Assert.Equal("ReviewState", LogProperties.ReviewState);
  }

  [Fact]
  public void NoPropertyNameContainsWhitespace()
  {
    var properties = new[]
    {
      LogProperties.ProjectId,
      LogProperties.OperationId,
      LogProperties.ApplicationUserId,
      LogProperties.UseCase,
      LogProperties.AttemptId,
      LogProperties.ProviderKey,
      LogProperties.FailureClassification,
      LogProperties.DurationMs,
      LogProperties.ImportSessionId,
      LogProperties.ImportedSourceId,
      LogProperties.ModelRunId,
      LogProperties.ProposalId,
      LogProperties.ReportId,
      LogProperties.InspectionSessionId,
      LogProperties.CorrelationId,
      LogProperties.ContentHash,
      LogProperties.FileName,
      LogProperties.DeclaredMediaType,
      LogProperties.ProcessorKey,
      LogProperties.CandidateCount,
      LogProperties.FragmentCandidateId,
      LogProperties.ParserRunId,
      LogProperties.ReviewState,
    };

    Assert.All(properties, name => Assert.DoesNotContain(" ", name));
  }
}
