using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SPINbuster.Infrastructure.Persistence.Records;

namespace SPINbuster.Infrastructure.Persistence;

public sealed class KnowledgeDocumentDeferredReferenceHandler : IDeferredReferenceHandler
{
  public bool CanHandle(EntityEntry entry)
  {
    return entry.Entity is KnowledgeDocumentRecord document
      && entry.State == EntityState.Added
      && document.CurrentAuthoritativeRevisionId is not null;
  }

  public IReadOnlyList<DeferredReferenceInfo> Extract(EntityEntry entry)
  {
    var document = (KnowledgeDocumentRecord)entry.Entity;
    return
    [
      new DeferredReferenceInfo(
        nameof(KnowledgeDocumentRecord.CurrentAuthoritativeRevisionId),
        document.CurrentAuthoritativeRevisionId),
    ];
  }

  public void Clear(EntityEntry entry, DeferredReferenceInfo reference)
  {
    entry.Property(reference.PropertyName).CurrentValue = null;
  }

  public void Restore(EntityEntry entry, DeferredReferenceInfo reference)
  {
    entry.Property(reference.PropertyName).CurrentValue = reference.Value;
    entry.Property(reference.PropertyName).IsModified = true;
  }
}
