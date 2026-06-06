using BinaryPrediction.Core.Common;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BinaryPrediction.Infrastructure.Services;

public class MarketEligibilityService : IMarketEligibilityService
{
    private readonly MarketFilteringSettings _settings;
    private readonly ILogger<MarketEligibilityService> _logger;

    public MarketEligibilityService(
        IOptions<MarketFilteringSettings> options,
        ILogger<MarketEligibilityService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public bool EvaluateEligibility(Market market, out string? reason)
    {
        reason = null;

        if (!market.Active)
        {
            reason = "Inactive markets are excluded.";
            return false;
        }

        if (market.Closed)
        {
            reason = "Closed markets are excluded.";
            return false;
        }

        if (market.Liquidity < _settings.MinimumLiquidity)
        {
            reason = "Liquidity below minimum threshold.";
            return false;
        }

        if (market.Volume < _settings.MinimumVolume)
        {
            reason = "Volume below minimum threshold.";
            return false;
        }

        if (!_settings.EligibleCategories.Contains(market.Category))
        {
            if (market.Category == BinaryPrediction.Core.Enums.MarketCategory.Other)
            {
                reason = "Market category could not be determined.";
            }
            else
            {
                reason = $"Market category '{market.Category}' is not eligible for analysis.";
            }
            return false;
        }

        if (market.QualityScore < _settings.MinimumQualityScore)
        {
            reason = $"Quality score ({market.QualityScore}) is below minimum ({_settings.MinimumQualityScore}).";
            return false;
        }

        var effectiveDate = market.EndDate ?? market.EstimatedResolutionDateUtc;
        if (effectiveDate.HasValue)
        {
            var maxDuration = TimeSpan.FromDays(_settings.MaximumMarketDurationDays);
            if (effectiveDate.Value - DateTimeOffset.UtcNow > maxDuration)
            {
                reason = "Market end date is too far in the future.";
                return false;
            }
        }
        else
        {
            reason = "Market resolution date could not be determined.";
            return false;
        }

        // Note: Reanalysis limits are checked during selection time.
        return true;
    }
}
