using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.Tests.Fakes;

internal sealed class FakePromotionRecordRepository : IPromotionRecordRepository
{
  private readonly Dictionary<PromotionRecordId, PromotionRecord> _records = [];

  public List<PromotionRecord> AddedRecords { get; } = [];

  public List<PromotionRecord> UpdatedRecords { get; } = [];

  public Task<PromotionRecord?> GetByIdAsync(
    PromotionRecordId promotionRecordId,
    CancellationToken cancellationToken = default)
  {
    _records.TryGetValue(promotionRecordId, out var record);
    return Task.FromResult(record);
  }

  public Task<PromotionRecord?> FindByIdentityHashAsync(
    ProjectId projectId,
    string identityHash,
    CancellationToken cancellationToken = default)
  {
    return Task.FromResult(
      _records.Values.FirstOrDefault(record =>
        record.Identity.ProjectId == projectId
        && string.Equals(record.Identity.Hash, identityHash, StringComparison.Ordinal)));
  }

  public Task AddAsync(
    PromotionRecord promotionRecord,
    CancellationToken cancellationToken = default)
  {
    _records[promotionRecord.Id] = promotionRecord;
    AddedRecords.Add(promotionRecord);
    return Task.CompletedTask;
  }

  public Task UpdateAsync(
    PromotionRecord promotionRecord,
    CancellationToken cancellationToken = default)
  {
    _records[promotionRecord.Id] = promotionRecord;
    UpdatedRecords.Add(promotionRecord);
    return Task.CompletedTask;
  }
}

internal sealed class FakePromotionAttemptRepository : IPromotionAttemptRepository
{
  private readonly Dictionary<PromotionAttemptId, PromotionAttempt> _attempts = [];

  public List<PromotionAttempt> AddedAttempts { get; } = [];

  public Task<PromotionAttempt?> GetByIdAsync(
    PromotionAttemptId promotionAttemptId,
    CancellationToken cancellationToken = default)
  {
    _attempts.TryGetValue(promotionAttemptId, out var attempt);
    return Task.FromResult(attempt);
  }

  public Task<PromotionAttempt?> GetLatestSuccessfulByRecordIdAsync(
    PromotionRecordId promotionRecordId,
    CancellationToken cancellationToken = default)
  {
    return Task.FromResult(
      _attempts.Values
        .Where(attempt =>
          attempt.RecordId == promotionRecordId
          && attempt.Outcome == PromotionAttemptOutcome.Promoted)
        .OrderByDescending(attempt => attempt.AttemptedAtUtc)
        .FirstOrDefault());
  }

  public Task AddAsync(
    PromotionAttempt promotionAttempt,
    CancellationToken cancellationToken = default)
  {
    _attempts[promotionAttempt.Id] = promotionAttempt;
    AddedAttempts.Add(promotionAttempt);
    return Task.CompletedTask;
  }
}

internal sealed class FakePromotionDiagnosticRepository : IPromotionDiagnosticRepository
{
  private readonly Dictionary<PromotionDiagnosticId, PromotionDiagnostic> _diagnostics = [];
  private readonly Dictionary<FragmentCandidateId, PromotionDiagnostic> _byCandidate = [];

  public List<PromotionDiagnostic> AddedDiagnostics { get; } = [];

  public Task<PromotionDiagnostic?> GetByIdAsync(
    PromotionDiagnosticId promotionDiagnosticId,
    CancellationToken cancellationToken = default)
  {
    _diagnostics.TryGetValue(promotionDiagnosticId, out var diagnostic);
    return Task.FromResult(diagnostic);
  }

  public Task<PromotionDiagnostic?> GetByFragmentCandidateAsync(
    FragmentCandidateId fragmentCandidateId,
    CancellationToken cancellationToken = default)
  {
    _byCandidate.TryGetValue(fragmentCandidateId, out var diagnostic);
    return Task.FromResult(diagnostic);
  }

  public Task<IReadOnlyCollection<PromotionDiagnostic>> GetByProjectAsync(
    ProjectId projectId,
    int maxResults,
    CancellationToken cancellationToken = default)
  {
    return Task.FromResult<IReadOnlyCollection<PromotionDiagnostic>>(
      _diagnostics.Values
        .Where(d => d.ProjectId == projectId)
        .OrderByDescending(d => d.PromotedAtUtc)
        .Take(maxResults)
        .ToArray());
  }

  public Task<PromotionDiagnostic?> FindSuccessfulByContentHashAsync(
    ProjectId projectId,
    string contentHash,
    string normalizedLocatorValue,
    CancellationToken cancellationToken = default)
  {
    return Task.FromResult(
      _diagnostics.Values.FirstOrDefault(d =>
        d.ProjectId == projectId
        && d.Status == PromotionDiagnosticStatus.Promoted));
  }

  public Task AddAsync(
    PromotionDiagnostic promotionDiagnostic,
    CancellationToken cancellationToken = default)
  {
    _diagnostics[promotionDiagnostic.Id] = promotionDiagnostic;
    _byCandidate[promotionDiagnostic.FragmentCandidateId] = promotionDiagnostic;
    AddedDiagnostics.Add(promotionDiagnostic);
    return Task.CompletedTask;
  }

  public Task UpdateAsync(
    PromotionDiagnostic promotionDiagnostic,
    CancellationToken cancellationToken = default)
  {
    _diagnostics[promotionDiagnostic.Id] = promotionDiagnostic;
    _byCandidate[promotionDiagnostic.FragmentCandidateId] = promotionDiagnostic;
    return Task.CompletedTask;
  }
}

internal sealed class FakePromotionProvenanceRepository : IPromotionProvenanceRepository
{
  private readonly Dictionary<PromotionProvenanceId, PromotionProvenance> _provenances = [];

  public List<PromotionProvenance> AddedProvenances { get; } = [];

  public Task<PromotionProvenance?> GetByRevisionIdAsync(
    KnowledgeDocumentRevisionId revisionId,
    CancellationToken cancellationToken = default)
  {
    return Task.FromResult(
      _provenances.Values.FirstOrDefault(p => p.PromotedRevisionId == revisionId));
  }

  public Task<PromotionProvenance?> GetByFragmentCandidateIdAsync(
    FragmentCandidateId fragmentCandidateId,
    CancellationToken cancellationToken = default)
  {
    return Task.FromResult(
      _provenances.Values.FirstOrDefault(p => p.FragmentCandidateId == fragmentCandidateId));
  }

  public Task AddAsync(
    PromotionProvenance provenance,
    CancellationToken cancellationToken = default)
  {
    _provenances[provenance.Id] = provenance;
    AddedProvenances.Add(provenance);
    return Task.CompletedTask;
  }
}
