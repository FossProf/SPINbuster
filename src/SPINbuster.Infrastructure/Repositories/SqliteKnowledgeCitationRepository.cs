using Microsoft.EntityFrameworkCore;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;

namespace SPINbuster.Infrastructure.Repositories;

public sealed class SqliteKnowledgeCitationRepository : IKnowledgeCitationRepository
{
  private readonly SpinbusterDbContext _dbContext;

  public SqliteKnowledgeCitationRepository(SpinbusterDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<IReadOnlyCollection<KnowledgeCitation>> GetByRevisionIdAsync(
    KnowledgeDocumentRevisionId knowledgeDocumentRevisionId,
    CancellationToken cancellationToken = default)
  {
    var records = await _dbContext.KnowledgeCitations
      .AsNoTracking()
      .Where(citation => citation.CitedRevisionId == knowledgeDocumentRevisionId)
      .ToArrayAsync(cancellationToken);

    return records
      .OrderBy(citation => citation.CreatedAtUtc)
      .ThenBy(citation => citation.Id)
      .Select(InfrastructureMapper.ToDomain)
      .ToArray();
  }

  public Task AddAsync(
    KnowledgeCitation knowledgeCitation,
    CancellationToken cancellationToken = default)
  {
    _dbContext.KnowledgeCitations.Add(InfrastructureMapper.ToRecord(knowledgeCitation));
    return Task.CompletedTask;
  }
}
