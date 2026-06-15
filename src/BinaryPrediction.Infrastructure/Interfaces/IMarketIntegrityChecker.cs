using System.Collections.Generic;
using System.Threading.Tasks;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Infrastructure.Persistence;

namespace BinaryPrediction.Infrastructure.Interfaces
{

public interface IMarketIntegrityChecker
{
    Task<IReadOnlyList<Market>> GetInvalidMarketsAsync();
}
}
