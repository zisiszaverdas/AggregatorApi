using AggregatorApi.Models;

namespace AggregatorApi.Clients;

public interface IApiClient
{
    string SourceName { get; }
    Task<IEnumerable<AggregatedItem>> FetchAsync(CancellationToken ct);
}