namespace SPINbuster.Application.Contracts;

public interface ICommandHandler<in TCommand, TResult>
  where TCommand : ICommand<TResult>
{
  Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
