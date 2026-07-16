using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SPINbuster.Application;
using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Persistence;

internal static class StronglyTypedIdValueConverters
{
  public static readonly ValueConverter<ProjectId, Guid> ProjectId = new(
    value => value.Value,
    value => new ProjectId(value));

  public static readonly ValueConverter<InspectionSessionId, Guid> InspectionSessionId = new(
    value => value.Value,
    value => new InspectionSessionId(value));

  public static readonly ValueConverter<FieldNoteId, Guid> FieldNoteId = new(
    value => value.Value,
    value => new FieldNoteId(value));

  public static readonly ValueConverter<EvidenceAttachmentId, Guid> EvidenceAttachmentId = new(
    value => value.Value,
    value => new EvidenceAttachmentId(value));

  public static readonly ValueConverter<ReportId, Guid> ReportId = new(
    value => value.Value,
    value => new ReportId(value));

  public static readonly ValueConverter<SaveTransactionId, Guid> SaveTransactionId = new(
    value => value.Value,
    value => new SaveTransactionId(value));

  public static readonly ValueConverter<AuditEventId, Guid> AuditEventId = new(
    value => value.Value,
    value => new AuditEventId(value));

  public static readonly ValueConverter<ContextManifestId, Guid> ContextManifestId = new(
    value => value.Value,
    value => new ContextManifestId(value));

  public static readonly ValueConverter<ModelRunId, Guid> ModelRunId = new(
    value => value.Value,
    value => new ModelRunId(value));

  public static readonly ValueConverter<ModelRunAttemptId, Guid> ModelRunAttemptId = new(
    value => value.Value,
    value => new ModelRunAttemptId(value));

  public static readonly ValueConverter<ProposalId, Guid> ProposalId = new(
    value => value.Value,
    value => new ProposalId(value));

  public static readonly ValueConverter<OperationId, Guid> OperationId = new(
    value => value.Value,
    value => new OperationId(value));
}
