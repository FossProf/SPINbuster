using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadImportedDocumentSource;

public sealed record LoadImportedDocumentSourceQuery(ImportedSourceId ImportedSourceId) : IQuery<LoadImportedDocumentSourceResult>;
