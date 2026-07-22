using Microsoft.EntityFrameworkCore;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;
using SPINbuster.Infrastructure.Persistence.Records;

namespace SPINbuster.Infrastructure.Repositories;

public sealed class SqliteParserDiagnosticRepository : IParserDiagnosticRepository
{
  private readonly SpinbusterDbContext _dbContext;

  public SqliteParserDiagnosticRepository(SpinbusterDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public Task AddRangeAsync(IReadOnlyList<ParserDiagnostic> diagnostics, CancellationToken cancellationToken = default)
  {
    var records = diagnostics.Select(ToRecord).ToArray();
    _dbContext.ParserDiagnostics.AddRange(records);
    return Task.CompletedTask;
  }

  public async Task<IReadOnlyList<ParserDiagnostic>> GetByParserRunAsync(ParserRunId parserRunId, CancellationToken cancellationToken = default)
  {
    var records = await _dbContext.ParserDiagnostics
      .AsNoTracking()
      .Where(d => d.ParserRunId == parserRunId)
      .ToArrayAsync(cancellationToken);

    return records.Select(ToDomain).ToArray();
  }

  public async Task<IReadOnlyList<ParserDiagnostic>> GetByParserRunAndCandidateAsync(ParserRunId parserRunId, string candidateRefValue, CancellationToken cancellationToken = default)
  {
    var records = await _dbContext.ParserDiagnostics
      .AsNoTracking()
      .Where(d => d.ParserRunId == parserRunId && d.CandidateRefValue == candidateRefValue)
      .ToArrayAsync(cancellationToken);

    return records.Select(ToDomain).ToArray();
  }

  private static ParserDiagnosticRecord ToRecord(ParserDiagnostic diagnostic)
  {
    return new ParserDiagnosticRecord
    {
      Id = diagnostic.Id,
      ParserRunId = diagnostic.ParserRunId,
      Severity = diagnostic.Severity,
      Code = diagnostic.Code,
      Message = diagnostic.Message,
      CreatedAtUtc = diagnostic.CreatedAtUtc,
      CandidateRefType = diagnostic.CandidateRefType,
      CandidateRefValue = diagnostic.CandidateRefValue,
      LocatorType = diagnostic.LocatorType,
      LocatorValue = diagnostic.LocatorValue,
    };
  }

  private static ParserDiagnostic ToDomain(ParserDiagnosticRecord record)
  {
    return new ParserDiagnostic(
      record.Id,
      record.ParserRunId,
      record.Severity,
      record.Code,
      record.Message,
      record.CreatedAtUtc,
      record.CandidateRefType,
      record.CandidateRefValue,
      record.LocatorType,
      record.LocatorValue);
  }
}
