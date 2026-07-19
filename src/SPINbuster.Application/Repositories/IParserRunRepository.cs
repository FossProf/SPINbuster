using SPINbuster.Domain;

namespace SPINbuster.Application.Repositories;

public interface IParserRunRepository
{
  Task<ParserRun?> GetByIdAsync(ParserRunId parserRunId, CancellationToken cancellationToken = default);

  Task<ParserRun?> GetBySourceAndParserAsync(
    ImportedSourceId importedSourceId,
    string parserKey,
    string parserVersion,
    string contractVersion,
    string contractHash,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyCollection<ParserRun>> GetByProjectAsync(
    ProjectId projectId,
    int maxResults,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyCollection<ParserRun>> GetByImportedSourceAsync(
    ImportedSourceId importedSourceId,
    int maxResults,
    CancellationToken cancellationToken = default);

  Task AddAsync(ParserRun parserRun, CancellationToken cancellationToken = default);

  Task UpdateAsync(ParserRun parserRun, CancellationToken cancellationToken = default);
}
