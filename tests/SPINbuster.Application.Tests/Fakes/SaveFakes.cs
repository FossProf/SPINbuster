using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.Tests.Fakes;

internal sealed class FakeSaveTransactionRepository : ISaveTransactionRepository
{
  private readonly Dictionary<SaveTransactionId, SaveTransaction> _saveTransactions = [];

  public Task<SaveTransaction?> GetByIdAsync(
    SaveTransactionId saveTransactionId,
    CancellationToken cancellationToken = default)
  {
    _saveTransactions.TryGetValue(saveTransactionId, out var saveTransaction);
    return Task.FromResult(saveTransaction);
  }

  public Task AddAsync(
    SaveTransaction saveTransaction,
    CancellationToken cancellationToken = default)
  {
    _saveTransactions[saveTransaction.Id] = saveTransaction;
    return Task.CompletedTask;
  }
}
