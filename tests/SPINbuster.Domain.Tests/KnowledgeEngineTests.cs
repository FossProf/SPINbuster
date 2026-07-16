namespace SPINbuster.Domain.Tests;

public sealed class KnowledgeEngineTests
{
  [Fact]
  public void KnowledgeIdentifiersRejectEmptyGuid()
  {
    Assert.Throws<DomainInvariantException>(() => new KnowledgeDocumentId(Guid.Empty));
    Assert.Throws<DomainInvariantException>(() => new KnowledgeDocumentRevisionId(Guid.Empty));
    Assert.Throws<DomainInvariantException>(() => new KnowledgeSourceId(Guid.Empty));
    Assert.Throws<DomainInvariantException>(() => new KnowledgeRelationshipId(Guid.Empty));
    Assert.Throws<DomainInvariantException>(() => new KnowledgeCitationId(Guid.Empty));
  }

  [Fact]
  public void KnowledgeDocumentRejectsEmptyTitle()
  {
    Assert.Throws<DomainInvariantException>(() => new KnowledgeDocument(
      KnowledgeDocumentId.New(),
      ProjectId.New(),
      KnowledgeDocumentType.Drawing,
      string.Empty,
      null,
      null,
      "reviewer@example.invalid",
      Timestamp(0)));
  }

  [Fact]
  public void KnowledgeRevisionRejectsEmptyLabel()
  {
    Assert.Throws<DomainInvariantException>(() => CreateRevision(revisionLabel: ""));
  }

  [Fact]
  public void KnowledgeRevisionSupportsSameDocumentSupersession()
  {
    var document = CreateDocument();
    var initialRevision = CreateRevision(documentId: document.Id, revisionLabel: "A");
    document.AddInitialRevision(initialRevision, "reviewer@example.invalid", Timestamp(1));

    var successor = CreateRevision(
      documentId: document.Id,
      revisionLabel: "B",
      supersedesRevisionId: initialRevision.Id);

    var outcome = document.SupersedeCurrentRevision(successor, "reviewer@example.invalid", Timestamp(2));

    Assert.Equal(KnowledgeRevisionLifecycle.Superseded, outcome.SupersededRevision.Lifecycle);
    Assert.Equal(KnowledgeRevisionLifecycle.CurrentAuthoritative, outcome.SuccessorRevision.Lifecycle);
    Assert.Equal(outcome.SuccessorRevision.Id, document.CurrentAuthoritativeRevisionId);
  }

  [Fact]
  public void KnowledgeRevisionRejectsCrossDocumentSupersession()
  {
    var firstDocument = CreateDocument();
    firstDocument.AddInitialRevision(CreateRevision(documentId: firstDocument.Id, revisionLabel: "A"), "reviewer@example.invalid", Timestamp(1));

    var exception = Assert.Throws<DomainInvariantException>(() =>
      firstDocument.SupersedeCurrentRevision(
        CreateRevision(
          documentId: firstDocument.Id,
          revisionLabel: "B",
          supersedesRevisionId: KnowledgeDocumentRevisionId.New()),
        "reviewer@example.invalid",
        Timestamp(2)));

    Assert.Contains("current authoritative revision", exception.Message, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public void KnowledgeRevisionRejectsSelfSupersession()
  {
    var revisionId = KnowledgeDocumentRevisionId.New();

    Assert.Throws<DomainInvariantException>(() => new KnowledgeDocumentRevision(
      revisionId,
      KnowledgeDocumentId.New(),
      KnowledgeSourceId.New(),
      "A",
      null,
      Timestamp(0),
      KnowledgeSourceAuthorityLevel.EngineerIssued,
      "content-hash",
      "metadata-hash",
      revisionId,
      null,
      null,
      Timestamp(1)));
  }

  [Fact]
  public void KnowledgeDocumentRejectsDuplicateRevisionLabels()
  {
    var document = CreateDocument();
    document.AddInitialRevision(CreateRevision(documentId: document.Id, revisionLabel: "A"), "reviewer@example.invalid", Timestamp(1));

    var duplicateLabelRevision = CreateRevision(documentId: document.Id, revisionLabel: "A");

    Assert.Throws<DomainInvariantException>(() =>
      document.AddInitialRevision(duplicateLabelRevision, "reviewer@example.invalid", Timestamp(2)));
  }

  [Fact]
  public void KnowledgeRelationshipEnforcesProjectIsolation()
  {
    var projectId = ProjectId.New();
    var otherProjectId = ProjectId.New();

    Assert.Throws<DomainInvariantException>(() => new KnowledgeRelationship(
      KnowledgeRelationshipId.New(),
      projectId,
      KnowledgeSubjectReference.ForDocument(projectId, KnowledgeDocumentId.New()),
      KnowledgeSubjectReference.ForDocument(otherProjectId, KnowledgeDocumentId.New()),
      KnowledgeRelationshipType.References,
      "Cross-project links are not allowed.",
      "reviewer@example.invalid",
      Timestamp(0)));
  }

  [Fact]
  public void KnowledgeRelationshipRejectsDuplicateRelationships()
  {
    var projectId = ProjectId.New();
    var source = KnowledgeSubjectReference.ForDocument(projectId, KnowledgeDocumentId.New());
    var target = KnowledgeSubjectReference.ForDocument(projectId, KnowledgeDocumentId.New());
    var existing = new KnowledgeRelationship(
      KnowledgeRelationshipId.New(),
      projectId,
      source,
      target,
      KnowledgeRelationshipType.References,
      "Reference one.",
      "reviewer@example.invalid",
      Timestamp(0));
    var duplicate = new KnowledgeRelationship(
      KnowledgeRelationshipId.New(),
      projectId,
      source,
      target,
      KnowledgeRelationshipType.References,
      "Reference two.",
      "reviewer@example.invalid",
      Timestamp(1));

    Assert.Throws<DomainInvariantException>(() => KnowledgeRelationship.EnsureNoDuplicate([existing], duplicate));
  }

  [Fact]
  public void ContradictoryRelationshipsRemainVisible()
  {
    var projectId = ProjectId.New();
    var source = KnowledgeSubjectReference.ForDocument(projectId, KnowledgeDocumentId.New());
    var target = KnowledgeSubjectReference.ForDocument(projectId, KnowledgeDocumentId.New());
    var support = new KnowledgeRelationship(
      KnowledgeRelationshipId.New(),
      projectId,
      source,
      target,
      KnowledgeRelationshipType.Supports,
      "Support exists.",
      "reviewer@example.invalid",
      Timestamp(0));
    var contradiction = new KnowledgeRelationship(
      KnowledgeRelationshipId.New(),
      projectId,
      source,
      target,
      KnowledgeRelationshipType.Contradicts,
      "Contradiction exists.",
      "reviewer@example.invalid",
      Timestamp(1));

    Assert.Single(support.AuditTrail);
    Assert.Equal(2, contradiction.AuditTrail.Count);
    Assert.Contains(contradiction.AuditTrail, auditEvent => auditEvent.EventType == "KnowledgeContradictionDetected");
  }

  [Fact]
  public void KnowledgeCitationValidatesLocatorValue()
  {
    Assert.Throws<DomainInvariantException>(() => new KnowledgeCitation(
      KnowledgeCitationId.New(),
      KnowledgeDocumentRevisionId.New(),
      KnowledgeCitationLocationType.Section,
      string.Empty,
      "content-hash",
      Timestamp(0),
      null));
  }

  [Fact]
  public void ArchivedKnowledgeDocumentMustBeRestoredBeforeNewActiveRevision()
  {
    var document = CreateDocument();
    document.Archive("reviewer@example.invalid", Timestamp(1));

    Assert.Throws<LifecycleTransitionException>(() =>
      document.AddInitialRevision(
        CreateRevision(documentId: document.Id, revisionLabel: "A"),
        "reviewer@example.invalid",
        Timestamp(2)));
  }

  private static KnowledgeDocument CreateDocument()
  {
    return new KnowledgeDocument(
      KnowledgeDocumentId.New(),
      ProjectId.New(),
      KnowledgeDocumentType.Drawing,
      "Clarifier Plan Set",
      "DWG-100",
      "Structural",
      "reviewer@example.invalid",
      Timestamp(0));
  }

  private static KnowledgeDocumentRevision CreateRevision(
    KnowledgeDocumentId? documentId = null,
    string revisionLabel = "A",
    KnowledgeDocumentRevisionId? supersedesRevisionId = null)
  {
    return new KnowledgeDocumentRevision(
      KnowledgeDocumentRevisionId.New(),
      documentId ?? KnowledgeDocumentId.New(),
      KnowledgeSourceId.New(),
      revisionLabel,
      new DateOnly(2026, 7, 16),
      Timestamp(0),
      KnowledgeSourceAuthorityLevel.EngineerIssued,
      $"content-hash-{revisionLabel}",
      $"metadata-hash-{revisionLabel}",
      supersedesRevisionId,
      "source-system-ref",
      "Revision notes.",
      Timestamp(1));
  }

  private static DateTimeOffset Timestamp(int hoursOffset)
  {
    return new DateTimeOffset(2026, 7, 16, 9 + hoursOffset, 0, 0, TimeSpan.Zero);
  }
}
