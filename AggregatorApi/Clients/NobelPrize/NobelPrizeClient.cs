using AggregatorApi.Models;
using Microsoft.Extensions.Caching.Hybrid;

namespace AggregatorApi.Clients.NobelPrize;

public class NobelPrizeClient : ApiClientBase<NobelPrizeResponse>
{
    public const string ClientName = "NobelPrize";
    public override string SourceName => ClientName;
    private static readonly string CacheKey = $"{ClientName}_Cache";
    private const string NobelPrizeUrl = "2.0/nobelPrizes?nobelPrizeYear=2024";

    public NobelPrizeClient(HttpClient httpClient, ILogger<NobelPrizeClient> logger, HybridCache cache)
        : base(httpClient, logger, cache, CacheKey) { }

    protected override string BuildUrl() => NobelPrizeUrl;

    protected override bool IsValidResponse(NobelPrizeResponse? response) => response?.NobelPrizes != null;

    protected override ApiClientResult BuildApiClientResult(NobelPrizeResponse response)
    {
        var items = response.NobelPrizes!.SelectMany(prize =>
            prize.Laureates?.Select(laureate => new AggregatedItem(
                SourceName,
                $"{laureate.FullName?.En ?? laureate.KnownName?.En ?? "Unknown"} ({prize.CategoryFullName?.En ?? prize.Category?.En ?? "Nobel Prize"})",
                laureate.Motivation?.En ?? "No Motivation",
                DateTime.TryParse(prize.DateAwarded, out var date) ? date : DateTime.MinValue,
                "NobelPrize"
            )) ?? new List<AggregatedItem>()
        );
        return new ApiClientResult(items);
    }
}
