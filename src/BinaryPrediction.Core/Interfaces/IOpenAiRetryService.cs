namespace BinaryPrediction.Core.Interfaces;

public interface IOpenAiRetryService
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default);
}
