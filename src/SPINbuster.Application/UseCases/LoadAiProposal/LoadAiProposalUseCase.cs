using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;

namespace SPINbuster.Application.UseCases.LoadAiProposal;

public sealed class LoadAiProposalUseCase : IQueryHandler<LoadAiProposalQuery, LoadAiProposalResult>
{
  private readonly IAiProposalRepository _proposalRepository;

  public LoadAiProposalUseCase(IAiProposalRepository proposalRepository)
  {
    _proposalRepository = proposalRepository;
  }

  public async Task<LoadAiProposalResult> HandleAsync(
    LoadAiProposalQuery query,
    CancellationToken cancellationToken = default)
  {
    var proposal = await _proposalRepository.GetByIdAsync(query.ProposalId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException("AiProposal", query.ProposalId.ToString());

    return new LoadAiProposalResult(
      proposal.Id,
      proposal.Status,
      proposal.ReportId,
      proposal.ProjectId,
      proposal.InspectionSessionId,
      proposal.ProviderId,
      proposal.ModelName,
      proposal.ModelDigest,
      proposal.PromptPackageId,
      proposal.PromptPackageVersion,
      proposal.OutputSchemaId,
      proposal.OutputSchemaVersion,
      proposal.ContextManifestId,
      proposal.ContextManifestHash,
      proposal.ConfidenceBand,
      proposal.GeneratedAtUtc,
      proposal.Warnings.ToArray(),
      proposal.UncertaintyCodes.ToArray(),
      proposal.ValidationFailures.ToArray(),
      proposal.ReferencedSourceIds.ToArray(),
      proposal.StructuredPayloadJson,
      proposal.AbstentionReason,
      proposal.ReviewDispositionNotes);
  }
}
