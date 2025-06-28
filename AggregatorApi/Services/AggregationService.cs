using AggregatorApi.Models;
using AggregatorApi.Clients;

namespace AggregatorApi.Services;

public class AggregationService : IAggregationService
{
    private readonly IEnumerable<IApiClient> _apiClients;

    public AggregationService(IEnumerable<IApiClient> apiClients)
    {
        _apiClients = apiClients;
    }

    public async Task<IEnumerable<AggregatedItem>> GetAggregatedDataAsync(AggregationRequest query, CancellationToken ct)
    {
        var tasks = _apiClients.Select(client => client.FetchAsync(ct));
        var results = await Task.WhenAll(tasks);
        var aggregatedData = results.SelectMany(x => x);

        // Filtering
        if (!string.IsNullOrEmpty(query.Category))
            aggregatedData = aggregatedData.Where(x => x.Category?.Equals(query.Category, StringComparison.OrdinalIgnoreCase) == true);
        if (query.Date.HasValue)
           aggregatedData = aggregatedData.Where(x => x.Date >= query.Date);

        // Sorting
        aggregatedData = query.SortBy?.ToLower() switch
        {
            "date" => query.Descending == true ? aggregatedData.OrderByDescending(x => x.Date) : aggregatedData.OrderBy(x => x.Date),
            "title" => query.Descending == true ? aggregatedData.OrderByDescending(x => x.Title) : aggregatedData.OrderBy(x => x.Title),
            "source" => query.Descending == true ? aggregatedData.OrderByDescending(x => x.Source) : aggregatedData.OrderBy(x => x.Source),
            "category" => query.Descending == true ? aggregatedData.OrderByDescending(x => x.Category) : aggregatedData.OrderBy(x => x.Category),
            _ => aggregatedData
        };

        return aggregatedData;
    }
}
