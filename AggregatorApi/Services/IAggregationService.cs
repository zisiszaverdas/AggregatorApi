using AggregatorApi.Models;

namespace AggregatorApi.Services;

/// <summary>
/// Provides methods to aggregate data from multiple external APIs and apply filtering, sorting, and error aggregation.
/// </summary>
public interface IAggregationService
{
    /// <summary>
    /// Retrieves aggregated data from all configured API clients, applying filtering, sorting, and error aggregation as specified in the query.
    /// </summary>
    /// <param name="query">The aggregation request containing filter and sort parameters.</param>
    /// <param name="ct">A cancellation token for the operation.</param>
    /// <returns>An <see cref="AggregatorResponse"/> containing the aggregated items and any error messages.</returns>
    Task<AggregatorResponse> GetAggregatedDataAsync(AggregationRequest query, CancellationToken ct);
}
