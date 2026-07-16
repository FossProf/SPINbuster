using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Internal;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.BuildReportProposalContext;

public sealed class BuildReportProposalContextUseCase
  : IQueryHandler<BuildReportProposalContextQuery, BuildReportProposalContextResult>
{
  private const string ContextPolicyVersion = "report-draft-context-policy/1.0";

  private readonly IClock _clock;
  private readonly IInspectionSessionRepository _inspectionSessionRepository;
  private readonly IProjectRepository _projectRepository;
  private readonly IReportRepository _reportRepository;

  public BuildReportProposalContextUseCase(
    IProjectRepository projectRepository,
    IInspectionSessionRepository inspectionSessionRepository,
    IReportRepository reportRepository,
    IClock clock)
  {
    _projectRepository = projectRepository;
    _inspectionSessionRepository = inspectionSessionRepository;
    _reportRepository = reportRepository;
    _clock = clock;
  }

  public async Task<BuildReportProposalContextResult> HandleAsync(
    BuildReportProposalContextQuery query,
    CancellationToken cancellationToken = default)
  {
    var report = await _reportRepository.GetByIdAsync(query.ReportId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(Report), query.ReportId.ToString());
    var project = await _projectRepository.GetByIdAsync(report.ProjectId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(Project), report.ProjectId.ToString());
    var inspectionSession = await _inspectionSessionRepository.GetByIdAsync(report.InspectionSessionId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(InspectionSession), report.InspectionSessionId.ToString());

    var assembly = ReportProposalContextAssembly.Create(
      project,
      inspectionSession,
      report,
      ContextPolicyVersion,
      _clock.UtcNow);

    return new BuildReportProposalContextResult(
      assembly.ContextManifest.Id,
      assembly.ContextManifest.ProjectId,
      assembly.ContextManifest.InspectionSessionId!.Value,
      assembly.ContextManifest.ContextPolicyVersion,
      assembly.ContextManifest.ManifestHash,
      assembly.ContextManifest.Status,
      assembly.ContextManifest.IncompleteReasons.ToArray(),
      assembly.ContextManifest.Entries.Select(entry => new BuildReportProposalContextSourceEntry(
        entry.Order,
        entry.SourceType,
        entry.SourceId,
        entry.SourceVersion,
        entry.ContentHash,
        entry.AuthorityClassification,
        entry.InclusionReason,
        entry.LimitationNotes,
        entry.IsSuperseded,
        entry.ConflictCodes.ToArray())).ToArray(),
      assembly.GovernedPromptContext);
  }
}
