using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.Tests.Fakes;

internal sealed class FakeReportRepository : IReportRepository
{
  private readonly Dictionary<ReportId, Report> _reports = [];
  private readonly Dictionary<OperationId, ReportId> _operationToReportIds = [];

  public List<Report> AddedReports { get; } = [];

  public Task<Report?> GetByIdAsync(ReportId reportId, CancellationToken cancellationToken = default)
  {
    _reports.TryGetValue(reportId, out var report);
    return Task.FromResult(report);
  }

  public Task<Report?> GetByOperationIdAsync(OperationId operationId, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(
      _operationToReportIds.TryGetValue(operationId, out var reportId) && _reports.TryGetValue(reportId, out var report)
        ? report
        : null);
  }

  public Task AddAsync(Report report, OperationId operationId, CancellationToken cancellationToken = default)
  {
    _reports[report.Id] = report;
    _operationToReportIds[operationId] = report.Id;
    AddedReports.Add(report);
    return Task.CompletedTask;
  }

  public Task<int> CountAsync(CancellationToken cancellationToken = default)
  {
    return Task.FromResult(_reports.Count);
  }
}

internal sealed class FakeContextManifestRepository : IContextManifestRepository
{
  private readonly Dictionary<ContextManifestId, ContextManifest> _manifests = [];

  public List<ContextManifest> AddedManifests { get; } = [];

  public Task<ContextManifest?> GetByIdAsync(
    ContextManifestId contextManifestId,
    CancellationToken cancellationToken = default)
  {
    _manifests.TryGetValue(contextManifestId, out var manifest);
    return Task.FromResult(manifest);
  }

  public Task AddAsync(ContextManifest contextManifest, CancellationToken cancellationToken = default)
  {
    _manifests[contextManifest.Id] = contextManifest;
    AddedManifests.Add(contextManifest);
    return Task.CompletedTask;
  }
}
