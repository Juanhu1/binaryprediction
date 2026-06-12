using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Enums;
using BinaryPrediction.Core.Interfaces;

namespace BinaryPrediction.Worker.Jobs;

/// <summary>
/// Background worker that handles automatic expiration and resolution of opportunities.
/// Runs on an hourly schedule.
/// </summary>
public class OpportunityLifecycleWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OpportunityLifecycleWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1); // hourly
    private readonly TimeSpan _expireAfter = TimeSpan.FromHours(24);
    private readonly TimeSpan _resolveAfter = TimeSpan.FromHours(48);

    public OpportunityLifecycleWorker(IServiceProvider serviceProvider, ILogger<OpportunityLifecycleWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OpportunityLifecycleWorker started with interval {Interval}", _interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IPredictionOpportunityRepository>();
                var lifecycleService = scope.ServiceProvider.GetRequiredService<IOpportunityLifecycleService>();
                var now = DateTimeOffset.UtcNow;

                // Expire opportunities that are Open or Active for >24h
                var expirables = (await repo.GetByStatusAsync(OpportunityStatus.Open, stoppingToken)).Concat(await repo.GetByStatusAsync(OpportunityStatus.Active, stoppingToken)).ToList();
                foreach (var opp in expirables)
                {
                    var age = now - opp.CreatedAtUtc;
                    if (age >= _expireAfter && opp.Status != OpportunityStatus.Expired)
                    {
                        await lifecycleService.ChangeStatusAsync(opp.Id, OpportunityStatus.Expired, "Auto-expired by worker", stoppingToken);
                        _logger.LogInformation("Opportunity {Id} expired automatically.", opp.Id);
                    }
                }

                // Resolve opportunities that are not Resolved and older than 48h
                var all = await repo.GetAllAsync(stoppingToken);
                foreach (var opp in all)
                {
                    var age = now - opp.CreatedAtUtc;
                    if (age >= _resolveAfter && opp.Status != OpportunityStatus.Resolved)
                    {
                        await lifecycleService.ChangeStatusAsync(opp.Id, OpportunityStatus.Resolved, "Auto-resolved by worker", stoppingToken);
                        _logger.LogInformation("Opportunity {Id} resolved automatically.", opp.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OpportunityLifecycleWorker loop.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
