using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.StartInspectionSession;

public sealed record StartInspectionSessionResult(
  InspectionSessionId InspectionSessionId,
  ProjectId ProjectId,
  InspectionSessionLifecycle Lifecycle,
  DateTimeOffset StartedAtUtc);
