using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.CreateReportDraft;

public sealed record CreateReportDraftCommand(
  OperationId OperationId,
  ProjectId ProjectId,
  InspectionSessionId InspectionSessionId,
  string Title,
  IReadOnlyCollection<FieldNoteId> SourceFieldNoteIds,
  IReadOnlyCollection<EvidenceAttachmentId> SourceEvidenceAttachmentIds,
  IReadOnlyCollection<CreateReportDraftSectionInput> Sections) : ICommand<CreateReportDraftResult>;

public sealed record CreateReportDraftSectionInput(string Heading, string Content);
