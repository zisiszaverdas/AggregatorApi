using AggregatorApi.Models;

namespace AggregatorApi.Services;

public interface IAggregationService
{
    Task<IEnumerable<AggregatedItem>> GetAggregatedDataAsync(AggregationRequest query, CancellationToken ct);
}
