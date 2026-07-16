using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadProjectKnowledgeSnapshot;

public sealed class LoadProjectKnowledgeSnapshotUseCase
  : IQueryHandler<LoadProjectKnowledgeSnapshotQuery, LoadProjectKnowledgeSnapshotResult>
{
  public const int DefaultRelationshipLimit = 64;
  public const int MaxRelationshipLimit = 512;

  private readonly IKnowledgeDocumentRepository _knowledgeDocumentRepository;
  private readonly IKnowledgeCitationRepository _knowledgeCitationRepository;
  private readonly IKnowledgeRelationshipRepository _knowledgeRelationshipRepository;
  private readonly IAuditEventQueryRepository _auditEventQueryRepository;

  public LoadProjectKnowledgeSnapshotUseCase(
    IKnowledgeDocumentRepository knowledgeDocumentRepository,
    IKnowledgeCitationRepository knowledgeCitationRepository,
    IKnowledgeRelationshipRepository knowledgeRelationshipRepository,
    IAuditEventQueryRepository auditEventQueryRepository)
  {
    _knowledgeDocumentRepository = knowledgeDocumentRepository;
    _knowledgeCitationRepository = knowledgeCitationRepository;
    _knowledgeRelationshipRepository = knowledgeRelationshipRepository;
    _auditEventQueryRepository = auditEventQueryRepository;
  }

  public async Task<LoadProjectKnowledgeSnapshotResult> HandleAsync(
    LoadProjectKnowledgeSnapshotQuery query,
    CancellationToken cancellationToken = default)
  {
    if (query.MaxRelationships < 1)
    {
      throw new DomainInvariantException($"{nameof(query.MaxRelationships)} must be at least 1.");
    }

    if (query.MaxRelationships > MaxRelationshipLimit)
    {
      throw new DomainInvariantException($"{nameof(query.MaxRelationships)} must not exceed {MaxRelationshipLimit}.");
    }

    var documents = await _knowledgeDocumentRepository.GetByProjectAsync(query.ProjectId, cancellationToken);
    var documentSnapshots = new List<ProjectKnowledgeDocumentSnapshot>(documents.Count);
    foreach (var document in documents
      .OrderBy(item => item.CanonicalTitle, StringComparer.OrdinalIgnoreCase)
      .ThenBy(item => item.Id.ToString(), StringComparer.Ordinal))
    {
      var revisionSnapshots = new List<ProjectKnowledgeRevisionSnapshot>(document.Revisions.Count);
      foreach (var revision in document.Revisions
        .OrderBy(item => item.CreatedAtUtc.UtcTicks)
        .ThenBy(item => item.Id.ToString(), StringComparer.Ordinal))
      {
        var citations = await _knowledgeCitationRepository.GetByRevisionIdAsync(revision.Id, cancellationToken);
        var revisionAudit = await LoadAuditHistoryAsync(nameof(KnowledgeDocumentRevision), revision.Id.ToString(), cancellationToken);
        revisionSnapshots.Add(new ProjectKnowledgeRevisionSnapshot(
          revision.Id,
          revision.RevisionLabel,
          revision.EffectiveDate,
          revision.ReceivedAtUtc,
          revision.SourceAuthority,
          revision.VerificationStatus,
          revision.Lifecycle,
          revision.ContentHash,
          revision.MetadataHash,
          revision.SupersedesRevisionId,
          revision.SupersededByRevisionId,
          revision.SourceSystemReference,
          revision.DescriptiveNotes,
          revision.CreatedAtUtc,
          revision.IngestionStatus,
          citations
            .OrderBy(item => item.CreatedAtUtc.UtcTicks)
            .ThenBy(item => item.Id.ToString(), StringComparer.Ordinal)
            .Select(item => new ProjectKnowledgeCitationSnapshot(
              item.Id,
              item.LocatorType,
              item.LocatorValue,
              item.RevisionContentHash,
              item.CreatedAtUtc,
              item.QuotedOrSummarizedText))
            .ToArray(),
          revisionAudit));
      }

      documentSnapshots.Add(new ProjectKnowledgeDocumentSnapshot(
        document.Id,
        document.DocumentType,
        document.CanonicalTitle,
        document.ExternalReferenceNumber,
        document.DisciplineOrCategory,
        document.Lifecycle,
        document.CurrentAuthoritativeRevisionId,
        revisionSnapshots,
        await LoadAuditHistoryAsync(nameof(KnowledgeDocument), document.Id.ToString(), cancellationToken)));
    }

    var relationships = await LoadRelationshipsAsync(query.ProjectId, documents, query.MaxRelationships, cancellationToken);
    return new LoadProjectKnowledgeSnapshotResult(query.ProjectId, documentSnapshots, relationships);
  }

  private async Task<IReadOnlyList<ProjectKnowledgeRelationshipSnapshot>> LoadRelationshipsAsync(
    ProjectId projectId,
    IReadOnlyCollection<KnowledgeDocument> documents,
    int maxRelationships,
    CancellationToken cancellationToken)
  {
    var relationships = new Dictionary<KnowledgeRelationshipId, KnowledgeRelationship>();
    foreach (var subject in documents
      .SelectMany(document => EnumerateSubjects(projectId, document))
      .Distinct())
    {
      var remaining = maxRelationships - relationships.Count;
      if (remaining <= 0)
      {
        break;
      }

      var relatedItems = await _knowledgeRelationshipRepository.GetBySubjectAsync(projectId, subject, remaining, cancellationToken);
      foreach (var relationship in relatedItems)
      {
        relationships.TryAdd(relationship.Id, relationship);
        if (relationships.Count >= maxRelationships)
        {
          break;
        }
      }
    }

    return relationships.Values
      .OrderBy(item => item.CreatedAtUtc.UtcTicks)
      .ThenBy(item => item.Id.ToString(), StringComparer.Ordinal)
      .Select(item => new ProjectKnowledgeRelationshipSnapshot(
        item.Id,
        item.RelationshipType,
        new ProjectKnowledgeSubjectSnapshot(
          item.Source.SubjectKind,
          item.Source.ToStableKey(),
          item.Source.DocumentId,
          item.Source.RevisionId),
        new ProjectKnowledgeSubjectSnapshot(
          item.Target.SubjectKind,
          item.Target.ToStableKey(),
          item.Target.DocumentId,
          item.Target.RevisionId),
        item.EvidenceOrRationale,
        item.VerificationStatus,
        item.CreatedAtUtc,
        item.AuditTrail
          .OrderBy(audit => audit.OccurredAtUtc)
          .ThenBy(audit => audit.Id.Value)
          .Select(MapAuditEntry)
          .ToArray()))
      .ToArray();
  }

  private async Task<IReadOnlyList<ProjectKnowledgeAuditEntry>> LoadAuditHistoryAsync(
    string subjectType,
    string subjectId,
    CancellationToken cancellationToken)
  {
    var auditEntries = await _auditEventQueryRepository.GetBySubjectAsync(subjectType, subjectId, cancellationToken);
    return auditEntries
      .OrderBy(item => item.OccurredAtUtc.UtcTicks)
      .ThenBy(item => item.Id.ToString(), StringComparer.Ordinal)
      .Select(MapAuditEntry)
      .ToArray();
  }

  private static IEnumerable<KnowledgeSubjectReference> EnumerateSubjects(ProjectId projectId, KnowledgeDocument document)
  {
    yield return KnowledgeSubjectReference.ForDocument(projectId, document.Id);

    foreach (var revision in document.Revisions)
    {
      yield return KnowledgeSubjectReference.ForRevision(projectId, revision.Id);
    }
  }

  private static ProjectKnowledgeAuditEntry MapAuditEntry(AuditEvent auditEvent)
  {
    return new ProjectKnowledgeAuditEntry(
      auditEvent.Id,
      auditEvent.EventType,
      auditEvent.Actor,
      auditEvent.OccurredAtUtc,
      auditEvent.Description);
  }
}
