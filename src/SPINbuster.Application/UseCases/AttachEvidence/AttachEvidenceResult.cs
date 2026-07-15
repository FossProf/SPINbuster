using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.AttachEvidence;

public sealed record AttachEvidenceResult(
  EvidenceAttachmentId EvidenceAttachmentId,
  InspectionSessionId InspectionSessionId,
  DateTimeOffset CapturedAtUtc);
