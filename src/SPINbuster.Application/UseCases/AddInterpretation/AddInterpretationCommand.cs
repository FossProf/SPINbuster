using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.AddInterpretation;

public sealed record AddInterpretationCommand(
  InspectionSessionId InspectionSessionId,
  EvidenceAttachmentId EvidenceAttachmentId,
  string Summary) : ICommand<AddInterpretationResult>;
