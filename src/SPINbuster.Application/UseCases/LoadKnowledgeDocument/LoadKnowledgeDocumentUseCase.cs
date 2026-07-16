using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadKnowledgeDocument;

public sealed class LoadKnowledgeDocumentUseCase
  : IQueryHandler<LoadKnowledgeDocumentQuery, LoadKnowledgeDocumentResult>
{
  private readonly IKnowledgeDocumentRepository _knowledgeDocumentRepository;

  public LoadKnowledgeDocumentUseCase(IKnowledgeDocumentRepository knowledgeDocumentRepository)
  {
    _knowledgeDocumentRepository = knowledgeDocumentRepository;
  }

  public async Task<LoadKnowledgeDocumentResult> HandleAsync(
    LoadKnowledgeDocumentQuery query,
    CancellationToken cancellationToken = default)
  {
    var knowledgeDocument = await _knowledgeDocumentRepository.GetByIdAsync(query.KnowledgeDocumentId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(KnowledgeDocument), query.KnowledgeDocumentId.ToString());

    return new LoadKnowledgeDocumentResult(
      knowledgeDocument.Id,
      knowledgeDocument.ProjectId,
      knowledgeDocument.DocumentType,
      knowledgeDocument.CanonicalTitle,
      knowledgeDocument.ExternalReferenceNumber,
      knowledgeDocument.DisciplineOrCategory,
      knowledgeDocument.CurrentAuthoritativeRevisionId,
      knowledgeDocument.Lifecycle,
      knowledgeDocument.Revisions
        .OrderBy(revision => revision.CreatedAtUtc)
        .Select(revision => new KnowledgeDocumentRevisionSummary(
          revision.Id,
          revision.RevisionLabel,
          revision.Lifecycle,
          revision.VerificationStatus,
          revision.SourceAuthority,
          revision.ReceivedAtUtc))
        .ToArray(),
      knowledgeDocument.AuditTrail
        .OrderBy(auditEvent => auditEvent.OccurredAtUtc)
        .ThenBy(auditEvent => auditEvent.Id.ToString(), StringComparer.Ordinal)
        .Select(ToAuditSnapshot)
        .ToArray());
  }

  private static KnowledgeAuditEntrySnapshot ToAuditSnapshot(AuditEvent auditEvent)
  {
    return new KnowledgeAuditEntrySnapshot(
      auditEvent.Id,
      auditEvent.EventType,
      auditEvent.Actor,
      auditEvent.OccurredAtUtc,
      auditEvent.Description);
  }
}
