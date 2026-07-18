using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadDocumentProcessingHistory;

public sealed record LoadDocumentProcessingHistoryQuery(ImportedSourceId ImportedSourceId, int MaxResults) : IQuery<LoadDocumentProcessingHistoryResult>;
