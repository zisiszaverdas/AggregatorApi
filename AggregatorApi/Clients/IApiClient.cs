using AggregatorApi.Models;

namespace AggregatorApi.Clients;

public interface IApiClient
{
    string SourceName { get; }
    Task<ApiClientResult> FetchAsync(CancellationToken ct);
}