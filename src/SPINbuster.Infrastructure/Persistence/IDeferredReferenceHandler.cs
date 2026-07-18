using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace SPINbuster.Infrastructure.Persistence;

/// <summary>
/// Handles deferred reference restoration for entity types that have circular
/// FK dependencies during insert. Implementations know how to null out and
/// restore specific FK values across a two-pass SaveChanges cycle.
/// </summary>
public interface IDeferredReferenceHandler
{
  bool CanHandle(EntityEntry entry);
  IReadOnlyList<DeferredReferenceInfo> Extract(EntityEntry entry);
  void Clear(EntityEntry entry, DeferredReferenceInfo reference);
  void Restore(EntityEntry entry, DeferredReferenceInfo reference);
}

public sealed record DeferredReferenceInfo(string PropertyName, object? Value);
