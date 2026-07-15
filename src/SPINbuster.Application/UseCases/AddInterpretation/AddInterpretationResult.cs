using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.AddInterpretation;

public sealed record AddInterpretationResult(
  InspectionSessionId InspectionSessionId,
  EvidenceAttachmentId EvidenceAttachmentId,
  string Summary,
  DateTimeOffset InterpretedAtUtc,
  string InterpretedBy);
