using AggregatorApi.Models;
using Microsoft.Extensions.Caching.Hybrid;

namespace AggregatorApi.Clients.NewsApi;

public class NewsApiClient : ApiClientBase<NewsApiResponse>
{
    public const string ClientName = "NewsApi";
    public override string SourceName => ClientName;
    private readonly string _apiKey;
    private readonly string _query = "greece";
    private readonly string _sortBy = "publishedAt";
    private readonly string _fromDate;
    private static readonly string CacheKey = $"{ClientName}_Cache";

    public NewsApiClient(HttpClient httpClient, ILogger<NewsApiClient> logger, HybridCache cache, IConfiguration config)
        : base(httpClient, logger, cache, CacheKey)
    {
        _apiKey = config["NewsApi:ApiKey"] ?? throw new InvalidOperationException("NewsApi:ApiKey not configured");
        _fromDate = DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd");
    }

    protected override string BuildUrl() => $"v2/everything?q={_query}&from={_fromDate}&sortBy={_sortBy}&apiKey={_apiKey}";

    protected override bool IsValidResponse(NewsApiResponse? response) => response?.Articles != null;

    protected override ApiClientResult BuildApiClientResult(NewsApiResponse response)
    {
        var items = response.Articles!.Select(article => new AggregatedItem(
            SourceName,
            article.Title ?? "No Title",
            article.Description ?? "No Description",
            DateTime.TryParse(article.PublishedAt, out var date) ? date : DateTime.MinValue,
            "News"
        ));
        return new ApiClientResult(items);
    }
}
