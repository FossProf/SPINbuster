using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.CaptureFieldNote;

public sealed record CaptureFieldNoteCommand(
  InspectionSessionId InspectionSessionId,
  string RawText) : ICommand<CaptureFieldNoteResult>;
