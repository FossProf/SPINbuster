using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.BeginDocumentImportSession;

public sealed record BeginDocumentImportSessionCommand(ProjectId ProjectId) : ICommand<BeginDocumentImportSessionResult>;
