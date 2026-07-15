using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.AttachEvidence;

public sealed record AttachEvidenceCommand(
  InspectionSessionId InspectionSessionId,
  string FileName,
  string MediaType,
  string StorageKey,
  string Checksum) : ICommand<AttachEvidenceResult>;
