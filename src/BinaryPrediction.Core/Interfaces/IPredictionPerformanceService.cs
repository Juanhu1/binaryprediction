using System;
using BinaryPrediction.Core.Entities;
using System.Threading.Tasks;

namespace BinaryPrediction.Core.Interfaces
{
    public interface IPredictionPerformanceService
    {
        /// <summary>
        /// Generates a daily performance snapshot for the specified date (UTC). If no date is provided, uses today's date (UTC).
        /// </summary>
        Task GenerateDailySnapshotAsync(DateTime? snapshotDateUtc = null);

        /// <summary>
        /// Retrieves the most recent performance snapshot.
        /// </summary>
        Task<PredictionPerformanceSnapshot?> GetCurrentPerformanceAsync();

        /// <summary>
        /// Retrieves a list of snapshots representing the performance trend between the given dates (inclusive).
        /// </summary>
        Task<IReadOnlyList<PredictionPerformanceSnapshot>> GetPerformanceTrendAsync(DateTime startDateUtc, DateTime endDateUtc);
    }
}
