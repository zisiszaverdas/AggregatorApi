using AggregatorApi.Models;

namespace AggregatorApi.Services;

public interface IAggregationService
{
    Task<AggregatorResponse> GetAggregatedDataAsync(AggregationRequest query, CancellationToken ct);
}
