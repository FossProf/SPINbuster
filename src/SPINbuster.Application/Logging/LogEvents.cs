using Microsoft.Extensions.Logging;

namespace SPINbuster.Application.Logging;

public static class LogEvents
{
  public static readonly EventId UseCaseStarting = new(1000, nameof(UseCaseStarting));
  public static readonly EventId UseCaseCompleted = new(1001, nameof(UseCaseCompleted));
  public static readonly EventId UseCaseFailed = new(1002, nameof(UseCaseFailed));
  public static readonly EventId UseCaseCancelled = new(1003, nameof(UseCaseCancelled));

  public static readonly EventId DocumentImportStarting = new(2000, nameof(DocumentImportStarting));
  public static readonly EventId DocumentImportCompleted = new(2001, nameof(DocumentImportCompleted));
  public static readonly EventId DocumentImportDuplicateDetected = new(2002, nameof(DocumentImportDuplicateDetected));
  public static readonly EventId DocumentImportFailed = new(2003, nameof(DocumentImportFailed));

  public static readonly EventId DocumentProcessingStarting = new(3000, nameof(DocumentProcessingStarting));
  public static readonly EventId DocumentProcessingCompleted = new(3001, nameof(DocumentProcessingCompleted));
  public static readonly EventId DocumentProcessingFailed = new(3002, nameof(DocumentProcessingFailed));
  public static readonly EventId DocumentProcessingCancelled = new(3003, nameof(DocumentProcessingCancelled));
  public static readonly EventId DocumentProcessingCandidateCreated = new(3004, nameof(DocumentProcessingCandidateCreated));

  public static readonly EventId ParserRunStarting = new(3100, nameof(ParserRunStarting));
  public static readonly EventId ParserRunCompleted = new(3101, nameof(ParserRunCompleted));
  public static readonly EventId ParserRunFailed = new(3102, nameof(ParserRunFailed));
  public static readonly EventId ParserRunCancelled = new(3103, nameof(ParserRunCancelled));

  public static readonly EventId AiProviderInvoked = new(4000, nameof(AiProviderInvoked));
  public static readonly EventId AiProviderCompleted = new(4001, nameof(AiProviderCompleted));
  public static readonly EventId AiProviderFailed = new(4002, nameof(AiProviderFailed));
  public static readonly EventId AiProviderCancelled = new(4003, nameof(AiProviderCancelled));
  public static readonly EventId AiProposalValidated = new(4004, nameof(AiProposalValidated));

  public static readonly EventId ContentStoreWriteStarting = new(5000, nameof(ContentStoreWriteStarting));
  public static readonly EventId ContentStoreWriteCompleted = new(5001, nameof(ContentStoreWriteCompleted));
  public static readonly EventId ContentStoreReadStarting = new(5002, nameof(ContentStoreReadStarting));
  public static readonly EventId ContentStoreReadCompleted = new(5003, nameof(ContentStoreReadCompleted));
  public static readonly EventId ContentStoreIntegrityMismatch = new(5004, nameof(ContentStoreIntegrityMismatch));

  public static readonly EventId TransactionCommitStarting = new(6000, nameof(TransactionCommitStarting));
  public static readonly EventId TransactionCommitCompleted = new(6001, nameof(TransactionCommitCompleted));
  public static readonly EventId TransactionCommitFailed = new(6002, nameof(TransactionCommitFailed));
  public static readonly EventId TransactionRolledBack = new(6003, nameof(TransactionRolledBack));
}
