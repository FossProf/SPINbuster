using SPINbuster.Domain;

namespace SPINbuster.Application.Repositories;

public interface ISaveTransactionRepository
{
  Task<SaveTransaction?> GetByIdAsync(
    SaveTransactionId saveTransactionId,
    CancellationToken cancellationToken = default);

  Task AddAsync(
    SaveTransaction saveTransaction,
    CancellationToken cancellationToken = default);
}
