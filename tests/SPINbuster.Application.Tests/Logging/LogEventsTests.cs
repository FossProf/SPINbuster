using System.Reflection;
using Microsoft.Extensions.Logging;
using SPINbuster.Application.Logging;

namespace SPINbuster.Application.Tests.Logging;

public sealed class LogEventsTests
{
  [Fact]
  public void UseCaseLifecycleEventsAreIn1000Range()
  {
    Assert.Equal(1000, LogEvents.UseCaseStarting.Id);
    Assert.Equal(1001, LogEvents.UseCaseCompleted.Id);
    Assert.Equal(1002, LogEvents.UseCaseFailed.Id);
    Assert.Equal(1003, LogEvents.UseCaseCancelled.Id);
  }

  [Fact]
  public void DocumentImportEventsAreIn2000Range()
  {
    Assert.Equal(2000, LogEvents.DocumentImportStarting.Id);
    Assert.Equal(2001, LogEvents.DocumentImportCompleted.Id);
    Assert.Equal(2002, LogEvents.DocumentImportDuplicateDetected.Id);
    Assert.Equal(2003, LogEvents.DocumentImportFailed.Id);
  }

  [Fact]
  public void DocumentProcessingEventsAreIn3000Range()
  {
    Assert.Equal(3000, LogEvents.DocumentProcessingStarting.Id);
    Assert.Equal(3001, LogEvents.DocumentProcessingCompleted.Id);
    Assert.Equal(3002, LogEvents.DocumentProcessingFailed.Id);
    Assert.Equal(3003, LogEvents.DocumentProcessingCancelled.Id);
    Assert.Equal(3004, LogEvents.DocumentProcessingCandidateCreated.Id);
  }

  [Fact]
  public void AiProviderEventsAreIn4000Range()
  {
    Assert.Equal(4000, LogEvents.AiProviderInvoked.Id);
    Assert.Equal(4001, LogEvents.AiProviderCompleted.Id);
    Assert.Equal(4002, LogEvents.AiProviderFailed.Id);
    Assert.Equal(4003, LogEvents.AiProviderCancelled.Id);
    Assert.Equal(4004, LogEvents.AiProposalValidated.Id);
  }

  [Fact]
  public void ContentStoreEventsAreIn5000Range()
  {
    Assert.Equal(5000, LogEvents.ContentStoreWriteStarting.Id);
    Assert.Equal(5001, LogEvents.ContentStoreWriteCompleted.Id);
    Assert.Equal(5002, LogEvents.ContentStoreReadStarting.Id);
    Assert.Equal(5003, LogEvents.ContentStoreReadCompleted.Id);
    Assert.Equal(5004, LogEvents.ContentStoreIntegrityMismatch.Id);
  }

  [Fact]
  public void TransactionEventsAreIn6000Range()
  {
    Assert.Equal(6000, LogEvents.TransactionCommitStarting.Id);
    Assert.Equal(6001, LogEvents.TransactionCommitCompleted.Id);
    Assert.Equal(6002, LogEvents.TransactionCommitFailed.Id);
    Assert.Equal(6003, LogEvents.TransactionRolledBack.Id);
  }

  [Fact]
  public void AllEventIdsAreUnique()
  {
    var fields = typeof(LogEvents)
      .GetFields(BindingFlags.Public | BindingFlags.Static)
      .Where(f => f.FieldType == typeof(EventId))
      .Select(f => ((EventId)f.GetValue(null)!).Id)
      .ToList();

    Assert.Equal(fields.Count, fields.Distinct().Count());
  }

  [Fact]
  public void AllEventNamesAreNonEmpty()
  {
    var fields = typeof(LogEvents)
      .GetFields(BindingFlags.Public | BindingFlags.Static)
      .Where(f => f.FieldType == typeof(EventId))
      .Select(f => ((EventId)f.GetValue(null)!).Name)
      .ToList();

    Assert.All(fields, name => Assert.False(string.IsNullOrWhiteSpace(name)));
  }
}
