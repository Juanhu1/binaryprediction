namespace BinaryPrediction.Core.Interfaces
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IPredictionBackfillService
    {
        Task<int> BackfillAsync(int batchSize = 1000, CancellationToken ct = default);
    }
}
