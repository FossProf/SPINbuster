using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.RequestDocumentProcessing;

public sealed record RequestDocumentProcessingCommand(ImportedSourceId ImportedSourceId, ProjectId ProjectId) : ICommand<RequestDocumentProcessingResult>;
