using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace BinaryPrediction.Infrastructure.External.Kalshi;

public class KalshiClient : IKalshiClient
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };
    private readonly HttpClient _httpClient;
    private readonly ILogger<KalshiClient> _logger;
    private readonly KalshiSettings _settings;

    public KalshiClient(
        HttpClient httpClient,
        IOptions<KalshiSettings> settings,
        ILogger<KalshiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<IReadOnlyList<KalshiMarketDto>> GetActiveMarketsAsync(CancellationToken cancellationToken)
    {
        var markets = new List<KalshiMarketDto>();
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Kalshi collection is disabled by configuration.");
            return markets;
        }

        var pageSize = Math.Clamp(_settings.PageSize, 1, 100);
        var maxPages = Math.Max(_settings.MaxPages, 1);
        string? cursor = null;

        for (var page = 0; page < maxPages; page++)
        {
            var requestUri = $"markets?limit={pageSize}&status=open&mve_filter=exclude";
            if (!string.IsNullOrEmpty(cursor))
            {
                requestUri += $"&cursor={Uri.EscapeDataString(cursor)}";
            }

            var pageResponse = await GetMarketsPageAsync(requestUri, cancellationToken);
            if (pageResponse == null || pageResponse.Markets.Count == 0)
            {
                break;
            }

            markets.AddRange(pageResponse.Markets);
            cursor = pageResponse.Cursor;

            if (string.IsNullOrEmpty(cursor) || pageResponse.Markets.Count < pageSize)
            {
                break;
            }
        }

        return markets;
    }

    private async Task<KalshiMarketsResponse?> GetMarketsPageAsync(string requestUri, CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var response = await _httpClient.GetAsync(requestUri, cancellationToken);

                if (IsTransient(response.StatusCode) && attempt < maxAttempts)
                {
                    _logger.LogWarning(
                        "Kalshi request returned transient status {StatusCode}; retrying attempt {Attempt}.",
                        response.StatusCode,
                        attempt + 1);
                    await DelayBeforeRetryAsync(attempt, cancellationToken);
                    continue;
                }

                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var page = await JsonSerializer.DeserializeAsync<KalshiMarketsResponse>(
                    stream,
                    JsonSerializerOptions,
                    cancellationToken);

                return page;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (attempt >= maxAttempts)
                {
                    _logger.LogError(ex, "Kalshi request failed after {MaxAttempts} attempts.", maxAttempts);
                    return null;
                }

                _logger.LogWarning(ex, "Kalshi request failed; retrying attempt {Attempt}.", attempt + 1);
                await DelayBeforeRetryAsync(attempt, cancellationToken);
            }
        }

        return null;
    }

    private static bool IsTransient(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.TooManyRequests
            or HttpStatusCode.RequestTimeout
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }

    private static Task DelayBeforeRetryAsync(int attempt, CancellationToken cancellationToken)
    {
        return Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
    }
}
