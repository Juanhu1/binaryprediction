using BinaryPrediction.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace BinaryPrediction.Infrastructure.Services;

public class OpenAiRetryService : IOpenAiRetryService
{
    private readonly ILogger<OpenAiRetryService> _logger;
    private const int MaxRetries = 3;

    public OpenAiRetryService(ILogger<OpenAiRetryService> logger)
    {
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        int retries = 0;
        int delaySeconds = 2; // 2s, 4s, 8s

        while (true)
        {
            try
            {
                return await action(cancellationToken);
            }
            catch (Exception ex)
            {
                // Let's retry on any exception representing a transient failure or 429
                if (ex is InvalidOperationException invEx && invEx.Message.Contains("429"))
                {
                    _logger.LogWarning("OpenAI Rate Limit hit (429). Attempt {Retry}/{MaxRetries}", retries + 1, MaxRetries);
                }
                else if (ex is HttpRequestException || ex is TaskCanceledException)
                {
                    _logger.LogWarning(ex, "OpenAI Network/Timeout failure. Attempt {Retry}/{MaxRetries}", retries + 1, MaxRetries);
                }
                else
                {
                    // If it's a parsing error or something else, we still retry just in case the model returns malformed JSON
                    _logger.LogWarning(ex, "OpenAI Operation failed. Attempt {Retry}/{MaxRetries}", retries + 1, MaxRetries);
                }

                if (retries >= MaxRetries)
                {
                    _logger.LogError("OpenAI Retry Limit exceeded. Throwing exception.");
                    throw;
                }

                _logger.LogInformation("Waiting {DelaySeconds}s before retrying...", delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);

                retries++;
                delaySeconds *= 2; // exponential backoff
            }
        }
    }
}
