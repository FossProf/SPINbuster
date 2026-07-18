using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.CompleteDocumentImportSession;

public sealed record CompleteDocumentImportSessionCommand(DocumentImportSessionId ImportSessionId) : ICommand<CompleteDocumentImportSessionResult>;
