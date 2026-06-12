using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Enums;
using BinaryPrediction.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace BinaryPrediction.Infrastructure.Services;

/// <summary>
/// Service that manages the lifecycle of PredictionOpportunity records.
/// </summary>
public class OpportunityLifecycleService : IOpportunityLifecycleService
{
    private readonly IPredictionOpportunityRepository _opportunityRepo;
    private readonly IOpportunityStatusHistoryRepository _historyRepo;
    private readonly ILogger<OpportunityLifecycleService> _logger;

    public OpportunityLifecycleService(
        IPredictionOpportunityRepository opportunityRepo,
        IOpportunityStatusHistoryRepository historyRepo,
        ILogger<OpportunityLifecycleService> logger)
    {
        _opportunityRepo = opportunityRepo;
        _historyRepo = historyRepo;
        _logger = logger;
    }

    public async Task<OpportunityDetailDto?> GetOpportunityAsync(Guid opportunityId, CancellationToken cancellationToken = default)
    {
        var opp = await _opportunityRepo.GetByIdAsync(opportunityId, cancellationToken);
        if (opp == null) return null;

        var history = await _historyRepo.GetByOpportunityIdAsync(opportunityId, cancellationToken);

        var dto = new OpportunityDetailDto
        {
            Id = opp.Id,
            PredictionId = opp.PredictionId,
            MarketId = opp.MarketId,
            MarketProbability = opp.MarketProbability,
            AiProbability = opp.AiProbability,
            ProbabilityGap = opp.ProbabilityGap,
            GapDirection = opp.GapDirection,
            HasEdge = opp.HasEdge,
            DetectedAtUtc = opp.DetectedAtUtc,
            Status = opp.Status,
            CreatedAtUtc = opp.CreatedAtUtc,
            LastStatusChangedAtUtc = opp.LastStatusChangedAtUtc,
            IgnoredAtUtc = opp.IgnoredAtUtc,
            ExpiredAtUtc = opp.ExpiredAtUtc,
            ResolvedAtUtc = opp.ResolvedAtUtc,
            StatusHistory = history.Select(h => new OpportunityStatusHistoryDto
            {
                Id = h.Id,
                PreviousStatus = h.PreviousStatus,
                NewStatus = h.NewStatus,
                Reason = h.Reason,
                ChangedAtUtc = h.ChangedAtUtc
            })
        };
        return dto;
    }

    public async Task ChangeStatusAsync(Guid opportunityId, OpportunityStatus newStatus, string reason, CancellationToken cancellationToken = default)
    {
        var opp = await _opportunityRepo.GetByIdAsync(opportunityId, cancellationToken);
        if (opp == null)
        {
            _logger.LogWarning("Opportunity {Id} not found when attempting to change status.", opportunityId);
            return;
        }

        var previousStatus = opp.Status;
        if (previousStatus == newStatus)
        {
            _logger.LogInformation("Opportunity {Id} status unchanged ({Status}); no history recorded.", opportunityId, newStatus);
            return;
        }
        opp.Status = newStatus;
        opp.LastStatusChangedAtUtc = DateTimeOffset.UtcNow;

        // Set timestamp fields based on the target status
        switch (newStatus)
        {
            case OpportunityStatus.Ignored:
                opp.IgnoredAtUtc = DateTimeOffset.UtcNow;
                break;
            case OpportunityStatus.Expired:
                opp.ExpiredAtUtc = DateTimeOffset.UtcNow;
                break;
            case OpportunityStatus.Resolved:
                opp.ResolvedAtUtc = DateTimeOffset.UtcNow;
                break;
            default:
                // Open or Active do not require extra timestamps
                break;
        }

        var history = new OpportunityStatusHistory
        {
            Id = Guid.NewGuid(),
            OpportunityId = opp.Id,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Reason = reason ?? string.Empty,
            ChangedAtUtc = DateTimeOffset.UtcNow
        };

        await _historyRepo.AddAsync(history, cancellationToken);
        await _opportunityRepo.UpdateAsync(opp, cancellationToken);
        await _historyRepo.SaveChangesAsync(cancellationToken);
        await _opportunityRepo.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Opportunity {Id} status changed from {Prev} to {New} (Reason: {Reason}).", opportunityId, previousStatus, newStatus, reason);
    }
}
