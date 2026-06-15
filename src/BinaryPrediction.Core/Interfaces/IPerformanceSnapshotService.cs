using System.Threading.Tasks;

namespace BinaryPrediction.Core.Interfaces
{
    public interface IPerformanceSnapshotService
    {
        Task GenerateDailySnapshotAsync();
        Task GenerateCategorySnapshotsAsync();
        Task GenerateCalibrationSnapshotsAsync();
        /// <summary>
        /// Deletes all analytics snapshots and rebuilds them from the current predictions data.
        /// </summary>
        Task RebuildAllSnapshotsAsync();
    }
}
