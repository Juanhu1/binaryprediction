using System.Threading.Tasks;

namespace BinaryPrediction.Core.Interfaces
{
    public interface IPerformanceSnapshotService
    {
        Task GenerateDailySnapshotAsync();
        Task GenerateCategorySnapshotsAsync();
        Task GenerateCalibrationSnapshotsAsync();
    }
}
