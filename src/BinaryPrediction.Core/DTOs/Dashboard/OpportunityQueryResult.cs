using System;

namespace BinaryPrediction.Core.DTOs.Dashboard
{
    public class OpportunityQueryResult : PaginatedResult<OpportunityDto>
    {
        public int OpenCount { get; set; }
        public int ActiveCount { get; set; }
        public int ExpiredCount { get; set; }
        public int IgnoredCount { get; set; }
        public int ResolvedCount { get; set; }
        public int TotalOpportunityRecords { get; set; }
        public int UniqueMarketsWithOpportunities { get; set; }
        public int CurrentActiveOpportunities { get; set; }
    }
}
