using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadPromotionDiagnostic;

public sealed class LoadPromotionDiagnosticUseCase
  : IQueryHandler<LoadPromotionDiagnosticQuery, LoadPromotionDiagnosticResult>
{
  private readonly IPromotionDiagnosticRepository _promotionDiagnosticRepository;

  public LoadPromotionDiagnosticUseCase(IPromotionDiagnosticRepository promotionDiagnosticRepository)
  {
    _promotionDiagnosticRepository = promotionDiagnosticRepository;
  }

  public async Task<LoadPromotionDiagnosticResult> HandleAsync(
    LoadPromotionDiagnosticQuery query,
    CancellationToken cancellationToken = default)
  {
    var diagnostic = await _promotionDiagnosticRepository.GetByIdAsync(query.PromotionDiagnosticId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(PromotionDiagnostic), query.PromotionDiagnosticId.ToString());

    return new LoadPromotionDiagnosticResult(
      diagnostic.Id,
      diagnostic.FragmentCandidateId,
      diagnostic.ParserRunId,
      diagnostic.ProjectId,
      diagnostic.Status,
      diagnostic.FailureReason,
      diagnostic.KnowledgeDocumentId,
      diagnostic.KnowledgeDocumentRevisionId,
      diagnostic.KnowledgeCitationId,
      diagnostic.SupersededExistingRevision,
      diagnostic.SupersededRevisionId,
      diagnostic.PromotedAtUtc);
  }
}
