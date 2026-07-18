using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadDocumentImportSession;

public sealed record LoadDocumentImportSessionQuery(DocumentImportSessionId ImportSessionId) : IQuery<LoadDocumentImportSessionResult>;
