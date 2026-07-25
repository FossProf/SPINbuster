using System.Reflection;

namespace SPINbuster.Domain.Tests;

public sealed class PromotionProvenanceTests
{
  private static readonly DateTimeOffset BaseTime = new(2026, 7, 20, 10, 0, 0, TimeSpan.Zero);

  [Fact]
  public void ValidConstructionSucceedsWithAllRequiredArguments()
  {
    var provenance = CreateProvenance();

    Assert.NotNull(provenance);
    Assert.NotEqual(Guid.Empty, provenance.Id.Value);
  }

  [Fact]
  public void CrossProjectRelationshipWithImportedSourceIsRejected()
  {
    var projectId = ProjectId.New();
    var otherProjectId = ProjectId.New();

    Assert.Throws<DomainInvariantException>(() => new KnowledgeRelationship(
      KnowledgeRelationshipId.New(),
      projectId,
      KnowledgeSubjectReference.ForImportedSource(projectId, ImportedSourceId.New()),
      KnowledgeSubjectReference.ForImportedSource(otherProjectId, ImportedSourceId.New()),
      KnowledgeRelationshipType.References,
      "Cross-project import source links are not allowed.",
      "reviewer@example.invalid",
      BaseTime));
  }

  [Fact]
  public void AllProvenanceFieldsAreStoredAndAccessible()
  {
    var id = PromotionProvenanceId.New();
    var projectId = ProjectId.New();
    var promotedRevisionId = KnowledgeDocumentRevisionId.New();
    var diagnosticId = PromotionDiagnosticId.New();
    var fragmentCandidateId = FragmentCandidateId.New();
    var parserRunId = ParserRunId.New();
    var importedSourceId = ImportedSourceId.New();
    var promotionAttemptId = PromotionAttemptId.New();
    var reviewedAt = BaseTime.AddHours(1);

    var provenance = new PromotionProvenance(
      id,
      projectId,
      promotedRevisionId,
      diagnosticId,
      fragmentCandidateId,
      "fragment-hash",
      FragmentCandidateReviewState.HumanAccepted,
      "reviewer@example.invalid",
      reviewedAt,
      parserRunId,
      "pdf-parser",
      "2.1.0",
      "1.0.0",
      "contract-hash",
      importedSourceId,
      "import-hash",
      "identity-hash",
      promotionAttemptId,
      "promoter@example.invalid",
      BaseTime);

    Assert.Equal(id, provenance.Id);
    Assert.Equal(projectId, provenance.ProjectId);
    Assert.Equal(promotedRevisionId, provenance.PromotedRevisionId);
    Assert.Equal(diagnosticId, provenance.DiagnosticId);
    Assert.Equal(fragmentCandidateId, provenance.FragmentCandidateId);
    Assert.Equal("fragment-hash", provenance.FragmentSourceContentHash);
    Assert.Equal(FragmentCandidateReviewState.HumanAccepted, provenance.ReviewState);
    Assert.Equal("reviewer@example.invalid", provenance.ReviewedBy);
    Assert.Equal(reviewedAt, provenance.ReviewedAtUtc);
    Assert.Equal(parserRunId, provenance.ParserRunId);
    Assert.Equal("pdf-parser", provenance.ParserKey);
    Assert.Equal("2.1.0", provenance.ParserVersion);
    Assert.Equal("1.0.0", provenance.ParserContractVersion);
    Assert.Equal("contract-hash", provenance.ParserContractHash);
    Assert.Equal(importedSourceId, provenance.ImportedSourceId);
    Assert.Equal("import-hash", provenance.ImportedSourceContentHash);
    Assert.Equal("identity-hash", provenance.PromotionIdentityHash);
    Assert.Equal(promotionAttemptId, provenance.PromotionAttemptId);
    Assert.Equal("promoter@example.invalid", provenance.PromotedBy);
    Assert.Equal(BaseTime, provenance.PromotedAtUtc);
  }

  [Fact]
  public void ForImportedSourceCreatesCorrectStronglyTypedSubjectReference()
  {
    var projectId = ProjectId.New();
    var importedSourceId = ImportedSourceId.New();

    var reference = KnowledgeSubjectReference.ForImportedSource(projectId, importedSourceId);

    Assert.Equal(KnowledgeSubjectKind.ImportedSource, reference.SubjectKind);
    Assert.Equal(importedSourceId, reference.ImportedSourceId);
    Assert.Null(reference.DocumentId);
    Assert.Null(reference.RevisionId);
    Assert.Equal($"ImportedSource:{importedSourceId}", reference.ToStableKey());
  }

  [Fact]
  public void RehydratePreservesAllProvenanceFields()
  {
    var id = PromotionProvenanceId.New();
    var projectId = ProjectId.New();
    var promotedRevisionId = KnowledgeDocumentRevisionId.New();
    var diagnosticId = PromotionDiagnosticId.New();
    var fragmentCandidateId = FragmentCandidateId.New();
    var parserRunId = ParserRunId.New();
    var importedSourceId = ImportedSourceId.New();
    var promotionAttemptId = PromotionAttemptId.New();
    var reviewedAt = BaseTime.AddHours(1);

    var provenance = PromotionProvenance.Rehydrate(
      id,
      projectId,
      promotedRevisionId,
      diagnosticId,
      fragmentCandidateId,
      "fragment-hash",
      FragmentCandidateReviewState.HumanAccepted,
      "reviewer@example.invalid",
      reviewedAt,
      parserRunId,
      "pdf-parser",
      "2.1.0",
      "1.0.0",
      "contract-hash",
      importedSourceId,
      "import-hash",
      "identity-hash",
      promotionAttemptId,
      "promoter@example.invalid",
      BaseTime);

    Assert.Equal(id, provenance.Id);
    Assert.Equal(projectId, provenance.ProjectId);
    Assert.Equal(promotedRevisionId, provenance.PromotedRevisionId);
    Assert.Equal(diagnosticId, provenance.DiagnosticId);
    Assert.Equal(fragmentCandidateId, provenance.FragmentCandidateId);
    Assert.Equal("fragment-hash", provenance.FragmentSourceContentHash);
    Assert.Equal(FragmentCandidateReviewState.HumanAccepted, provenance.ReviewState);
    Assert.Equal("reviewer@example.invalid", provenance.ReviewedBy);
    Assert.Equal(reviewedAt, provenance.ReviewedAtUtc);
    Assert.Equal(parserRunId, provenance.ParserRunId);
    Assert.Equal("pdf-parser", provenance.ParserKey);
    Assert.Equal("2.1.0", provenance.ParserVersion);
    Assert.Equal("1.0.0", provenance.ParserContractVersion);
    Assert.Equal("contract-hash", provenance.ParserContractHash);
    Assert.Equal(importedSourceId, provenance.ImportedSourceId);
    Assert.Equal("import-hash", provenance.ImportedSourceContentHash);
    Assert.Equal("identity-hash", provenance.PromotionIdentityHash);
    Assert.Equal(promotionAttemptId, provenance.PromotionAttemptId);
    Assert.Equal("promoter@example.invalid", provenance.PromotedBy);
    Assert.Equal(BaseTime, provenance.PromotedAtUtc);
  }

  [Fact]
  public void RejectsNullFragmentSourceContentHash()
  {
    Assert.Throws<DomainInvariantException>(() => CreateProvenance(fragmentSourceContentHash: null!));
  }

  [Fact]
  public void RejectsEmptyFragmentSourceContentHash()
  {
    Assert.Throws<DomainInvariantException>(() => CreateProvenance(fragmentSourceContentHash: ""));
  }

  [Fact]
  public void RejectsWhitespaceFragmentSourceContentHash()
  {
    Assert.Throws<DomainInvariantException>(() => CreateProvenance(fragmentSourceContentHash: "   "));
  }

  [Fact]
  public void RejectsNullParserKey()
  {
    Assert.Throws<DomainInvariantException>(() => CreateProvenance(parserKey: null!));
  }

  [Fact]
  public void RejectsEmptyParserKey()
  {
    Assert.Throws<DomainInvariantException>(() => CreateProvenance(parserKey: ""));
  }

  [Fact]
  public void RejectsWhitespaceParserKey()
  {
    Assert.Throws<DomainInvariantException>(() => CreateProvenance(parserKey: "   "));
  }

  [Fact]
  public void RejectsNullParserVersion()
  {
    Assert.Throws<DomainInvariantException>(() => CreateProvenance(parserVersion: null!));
  }

  [Fact]
  public void RejectsEmptyParserVersion()
  {
    Assert.Throws<DomainInvariantException>(() => CreateProvenance(parserVersion: ""));
  }

  [Fact]
  public void RejectsWhitespaceParserVersion()
  {
    Assert.Throws<DomainInvariantException>(() => CreateProvenance(parserVersion: "   "));
  }

  [Fact]
  public void RejectsNullParserContractVersion()
  {
    Assert.Throws<DomainInvariantException>(() => CreateProvenance(parserContractVersion: null!));
  }

  [Fact]
  public void RejectsEmptyParserContractVersion()
  {
    Assert.Throws<DomainInvariantException>(() => CreateProvenance(parserContractVersion: ""));
  }

  [Fact]
  public void RejectsWhitespaceParserContractVersion()
  {
    Assert.Throws<DomainInvariantException>(() => CreateProvenance(parserContractVersion: "   "));
  }

  [Fact]
  public void RejectsNullParserContractHash()
  {
    Assert.Throws<DomainInvariantException>(() => CreateProvenance(parserContractHash: null!));
  }

  [Fact]
  public void RejectsEmptyParserContractHash()
  {
    Assert.Throws<DomainInvariantException>(() => CreateProvenance(parserContractHash: ""));
  }

  [Fact]
  public void RejectsWhitespaceParserContractHash()
  {
    Assert.Throws<DomainInvariantException>(() => CreateProvenance(parserContractHash: "   "));
  }

  [Fact]
  public void RejectsNullImportedSourceContentHash()
  {
    Assert.Throws<DomainInvariantException>(() => CreateProvenance(importedSourceContentHash: null!));
  }

  [Fact]
  public void RejectsEmptyImportedSourceContentHash()
  {
    Assert.Throws<DomainInvariantException>(() => CreateProvenance(importedSourceContentHash: ""));
  }

  [Fact]
  public void RejectsWhitespaceImportedSourceContentHash()
  {
    Assert.Throws<DomainInvariantException>(() => CreateProvenance(importedSourceContentHash: "   "));
  }

  [Fact]
  public void RejectsNullPromotionIdentityHash()
  {
    Assert.Throws<DomainInvariantException>(() => CreateProvenance(promotionIdentityHash: null!));
  }

  [Fact]
  public void RejectsEmptyPromotionIdentityHash()
  {
    Assert.Throws<DomainInvariantException>(() => CreateProvenance(promotionIdentityHash: ""));
  }

  [Fact]
  public void RejectsWhitespacePromotionIdentityHash()
  {
    Assert.Throws<DomainInvariantException>(() => CreateProvenance(promotionIdentityHash: "   "));
  }

  [Fact]
  public void RejectsNullPromotedBy()
  {
    Assert.Throws<DomainInvariantException>(() => CreateProvenance(promotedBy: null!));
  }

  [Fact]
  public void RejectsEmptyPromotedBy()
  {
    Assert.Throws<DomainInvariantException>(() => CreateProvenance(promotedBy: ""));
  }

  [Fact]
  public void RejectsWhitespacePromotedBy()
  {
    Assert.Throws<DomainInvariantException>(() => CreateProvenance(promotedBy: "   "));
  }

  [Fact]
  public void RejectsDefaultPromotedAtUtc()
  {
    Assert.Throws<DomainInvariantException>(() => new PromotionProvenance(
      PromotionProvenanceId.New(),
      ProjectId.New(),
      KnowledgeDocumentRevisionId.New(),
      PromotionDiagnosticId.New(),
      FragmentCandidateId.New(),
      "fragment-hash",
      FragmentCandidateReviewState.HumanAccepted,
      "reviewer@example.invalid",
      BaseTime.AddHours(1),
      ParserRunId.New(),
      "pdf-parser",
      "2.1.0",
      "1.0.0",
      "contract-hash",
      ImportedSourceId.New(),
      "import-hash",
      "identity-hash",
      PromotionAttemptId.New(),
      "promoter@example.invalid",
      default));
  }

  [Fact]
  public void AllPropertiesHavePrivateSettersAndAreImmutableAfterConstruction()
  {
    var provenance = CreateProvenance();
    var properties = typeof(PromotionProvenance).GetProperties(BindingFlags.Public | BindingFlags.Instance);

    foreach (var property in properties)
    {
      var setter = property.GetSetMethod(nonPublic: true);
      if (setter != null)
      {
        Assert.False(setter.IsPublic, $"Property {property.Name} should not have a public setter.");
      }
    }
  }

  [Fact]
  public void DiagnosticConflictTypeDefaultsToNone()
  {
    var diagnostic = new PromotionDiagnostic(
      PromotionDiagnosticId.New(),
      FragmentCandidateId.New(),
      ParserRunId.New(),
      ProjectId.New(),
      BaseTime);

    Assert.Equal(PromotionConflictType.None, diagnostic.ConflictType);
  }

  [Fact]
  public void RecordFailureWithAmbiguousDocumentMatchSetsConflictType()
  {
    var diagnostic = new PromotionDiagnostic(
      PromotionDiagnosticId.New(),
      FragmentCandidateId.New(),
      ParserRunId.New(),
      ProjectId.New(),
      BaseTime);

    diagnostic.RecordFailure("Ambiguous match", PromotionConflictType.AmbiguousDocumentMatch);

    Assert.Equal(PromotionConflictType.AmbiguousDocumentMatch, diagnostic.ConflictType);
    Assert.Equal(PromotionDiagnosticStatus.Failed, diagnostic.Status);
  }

  [Fact]
  public void RecordFailureWithHigherAuthoritySetsConflictType()
  {
    var diagnostic = new PromotionDiagnostic(
      PromotionDiagnosticId.New(),
      FragmentCandidateId.New(),
      ParserRunId.New(),
      ProjectId.New(),
      BaseTime);

    diagnostic.RecordFailure("Higher authority exists", PromotionConflictType.HigherAuthorityExists);

    Assert.Equal(PromotionConflictType.HigherAuthorityExists, diagnostic.ConflictType);
  }

  [Fact]
  public void RecordFailureWithTemporalOrderViolationSetsConflictType()
  {
    var diagnostic = new PromotionDiagnostic(
      PromotionDiagnosticId.New(),
      FragmentCandidateId.New(),
      ParserRunId.New(),
      ProjectId.New(),
      BaseTime);

    diagnostic.RecordFailure("Temporal order violated", PromotionConflictType.TemporalOrderViolation);

    Assert.Equal(PromotionConflictType.TemporalOrderViolation, diagnostic.ConflictType);
  }

  [Fact]
  public void RecordFailureWithoutConflictTypeDefaultsToNone()
  {
    var diagnostic = new PromotionDiagnostic(
      PromotionDiagnosticId.New(),
      FragmentCandidateId.New(),
      ParserRunId.New(),
      ProjectId.New(),
      BaseTime);

    diagnostic.RecordFailure("Generic failure");

    Assert.Equal(PromotionConflictType.None, diagnostic.ConflictType);
  }

  [Fact]
  public void ConcurrencyConflictExceptionInheritsFromDomainInvariantException()
  {
    var exception = new ConcurrencyConflictException("test message");
    Assert.IsAssignableFrom<DomainInvariantException>(exception);
    Assert.Equal("test message", exception.Message);
  }

  private static PromotionProvenance CreateProvenance(
    string fragmentSourceContentHash = "fragment-hash",
    string parserKey = "pdf-parser",
    string parserVersion = "2.1.0",
    string parserContractVersion = "1.0.0",
    string parserContractHash = "contract-hash",
    string importedSourceContentHash = "import-hash",
    string promotionIdentityHash = "identity-hash",
    string promotedBy = "promoter@example.invalid",
    DateTimeOffset? promotedAtUtc = null)
  {
    return new PromotionProvenance(
      PromotionProvenanceId.New(),
      ProjectId.New(),
      KnowledgeDocumentRevisionId.New(),
      PromotionDiagnosticId.New(),
      FragmentCandidateId.New(),
      fragmentSourceContentHash,
      FragmentCandidateReviewState.HumanAccepted,
      "reviewer@example.invalid",
      BaseTime.AddHours(1),
      ParserRunId.New(),
      parserKey,
      parserVersion,
      parserContractVersion,
      parserContractHash,
      ImportedSourceId.New(),
      importedSourceContentHash,
      promotionIdentityHash,
      PromotionAttemptId.New(),
      promotedBy,
      promotedAtUtc ?? BaseTime);
  }
}
