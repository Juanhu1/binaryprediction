using BinaryPrediction.Infrastructure.External.Polymarket.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BinaryPrediction.Infrastructure.External.Polymarket;

public class PolymarketClient : IPolymarketClient
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };
    private readonly HttpClient _httpClient;
    private readonly ILogger<PolymarketClient> _logger;
    private readonly PolymarketSettings _settings;

    public PolymarketClient(
        HttpClient httpClient,
        IOptions<PolymarketSettings> settings,
        ILogger<PolymarketClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<IReadOnlyList<PolymarketMarketDto>> GetActiveMarketsAsync(CancellationToken cancellationToken)
    {
        var markets = new List<PolymarketMarketDto>();
        var pageSize = Math.Clamp(_settings.PageSize, 1, 500);
        var maxPages = Math.Max(_settings.MaxPages, 1);

        for (var page = 0; page < maxPages; page++)
        {
            var offset = page * pageSize;
            var requestUri = $"markets?active=true&closed=false&limit={pageSize}&offset={offset}";
            var pageMarkets = await GetMarketsPageAsync(requestUri, cancellationToken);

            if (pageMarkets.Count == 0)
            {
                break;
            }

            markets.AddRange(pageMarkets);

            if (pageMarkets.Count < pageSize)
            {
                break;
            }
        }

        return markets;
    }

    private async Task<IReadOnlyList<PolymarketMarketDto>> GetMarketsPageAsync(string requestUri, CancellationToken cancellationToken)
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
                        "Polymarket request returned transient status {StatusCode}; retrying attempt {Attempt}.",
                        response.StatusCode,
                        attempt + 1);
                    await DelayBeforeRetryAsync(attempt, cancellationToken);
                    continue;
                }

                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var markets = await JsonSerializer.DeserializeAsync<IReadOnlyList<PolymarketMarketDto>>(
                    stream,
                    JsonSerializerOptions,
                    cancellationToken);

                return markets ?? [];
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (attempt >= maxAttempts)
                {
                    _logger.LogError(ex, "Polymarket request failed after {MaxAttempts} attempts.", maxAttempts);
                    return [];
                }

                _logger.LogWarning(ex, "Polymarket request failed; retrying attempt {Attempt}.", attempt + 1);
                await DelayBeforeRetryAsync(attempt, cancellationToken);
            }
        }

        return [];
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

    public async Task<PolymarketMarketDto?> GetMarketAsync(string slug, CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                // The endpoint to fetch a single market by slug might be `/markets/{slug}` or similar.
                // Assuming `/markets/{slug}` is valid based on standard REST practices.
                // Alternatively, query `markets?slug={slug}` and return the first match.
                var requestUri = $"markets?slug={slug}";
                using var response = await _httpClient.GetAsync(requestUri, cancellationToken);

                if (IsTransient(response.StatusCode) && attempt < maxAttempts)
                {
                    _logger.LogWarning(
                        "Polymarket GetMarketAsync returned transient status {StatusCode}; retrying attempt {Attempt}.",
                        response.StatusCode,
                        attempt + 1);
                    await DelayBeforeRetryAsync(attempt, cancellationToken);
                    continue;
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var markets = await JsonSerializer.DeserializeAsync<IReadOnlyList<PolymarketMarketDto>>(
                    stream,
                    JsonSerializerOptions,
                    cancellationToken);

                return markets?.FirstOrDefault();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (attempt >= maxAttempts)
                {
                    _logger.LogError(ex, "Polymarket GetMarketAsync request failed after {MaxAttempts} attempts for slug {Slug}.", maxAttempts, slug);
                    return null;
                }

                _logger.LogWarning(ex, "Polymarket GetMarketAsync request failed; retrying attempt {Attempt}.", attempt + 1);
                await DelayBeforeRetryAsync(attempt, cancellationToken);
            }
        }

        return null;
    }
}
