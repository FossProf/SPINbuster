using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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
}
