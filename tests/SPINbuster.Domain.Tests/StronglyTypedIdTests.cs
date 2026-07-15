namespace SPINbuster.Domain.Tests;

public sealed class StronglyTypedIdTests
{
  [Fact]
  public void ProjectIdRejectsEmptyGuid()
  {
    Assert.Throws<DomainInvariantException>(() => new ProjectId(Guid.Empty));
  }

  [Fact]
  public void StronglyTypedIdsGenerateNonEmptyValues()
  {
    Assert.NotEqual(Guid.Empty, ProjectId.New().Value);
    Assert.NotEqual(Guid.Empty, InspectionSessionId.New().Value);
    Assert.NotEqual(Guid.Empty, FieldNoteId.New().Value);
    Assert.NotEqual(Guid.Empty, EvidenceAttachmentId.New().Value);
    Assert.NotEqual(Guid.Empty, ReportId.New().Value);
    Assert.NotEqual(Guid.Empty, SaveTransactionId.New().Value);
    Assert.NotEqual(Guid.Empty, AuditEventId.New().Value);
  }
}
