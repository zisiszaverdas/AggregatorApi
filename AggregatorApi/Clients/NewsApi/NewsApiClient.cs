using AggregatorApi.Models;
using Microsoft.Extensions.Caching.Hybrid;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Text.Json;

namespace AggregatorApi.Clients.NewsApi;

public class NewsApiClient : IApiClient
{
    public const string ClientName = "NewsApi";
    public string SourceName => ClientName;
    private readonly string _apiKey;
    private readonly string _query = "greece";
    private readonly string _sortBy = "publishedAt";
    private readonly string _fromDate;
    private readonly HttpClient _httpClient;
    private readonly ILogger<NewsApiClient> _logger;
    private readonly HybridCache _cache;
    private static readonly string CacheKey = $"{ClientName}_Cache";
    private static readonly HybridCacheEntryOptions EntryOptions = new() { Expiration = TimeSpan.FromMinutes(1) };

    public NewsApiClient(HttpClient httpClient, ILogger<NewsApiClient> logger, HybridCache cache, IConfiguration config)
    {

        _httpClient = httpClient;
         _logger = logger;
         _cache = cache;
        _apiKey = config["NewsApi:ApiKey"] ?? throw new InvalidOperationException("NewsApi:ApiKey not configured");
        _fromDate = DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd");
    }


    public async Task<ApiClientResult> FetchAsync(CancellationToken ct)
    {
        var url = BuildUrl();
        NewsApiResponse? response = null;
        try
        {
            response = await _cache.GetOrCreateAsync(
                CacheKey,
                async (ct) => await FetchData(url, ct),
                EntryOptions);
        }
        catch (BrokenCircuitException ex)
        {
            var errorMessage = $"{SourceName}: Circuit breaker is open. External service is currently unavailable.";
            return ApiClientResult.CreateErrorResult(errorMessage, _logger, ex);
        }
        catch (TimeoutRejectedException ex)
        {
            var errorMessage = $"{SourceName}: Request timed out while fetching data from {SourceName} API.";
            return ApiClientResult.CreateErrorResult(errorMessage, _logger, ex);
        }
        catch (HttpRequestException ex)
        {
            var errorMessage = $"{SourceName}: Failed to fetch data from {SourceName} API.";
            return ApiClientResult.CreateErrorResult(errorMessage, _logger, ex);
        }
        catch (JsonException ex)
        {
            var errorMessage = $"{SourceName}: JSON deserialization error while processing {SourceName} API response.";
            return ApiClientResult.CreateErrorResult(errorMessage, _logger, ex);
        }
        catch (Exception ex)
        {
            var errorMessage = $"{SourceName}: Unexpected error while fetching data from {SourceName} API.";
            return ApiClientResult.CreateErrorResult(errorMessage, _logger, ex);
        }

        if (response == null)
        {
            var errorMessage = $"{SourceName}: issue fetching data from {SourceName} API. Status code was not successful.";
            return ApiClientResult.CreateErrorResult(errorMessage, _logger);
        }

        if (!IsValidResponse(response))
        {
            return ApiClientResult.CreateErrorResult($"{SourceName}: Error response JSON contains empty properties or could not be fetched.", _logger);
        }
        return BuildApiClientResult(response);
    }

    private async Task<NewsApiResponse?> FetchData(string url, CancellationToken ct)
    {
        using var resp = await _httpClient.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var errorResponse = await resp.Content.ReadAsStringAsync();
            throw new HttpRequestException($"{SourceName}: Failed to fetch data from {SourceName} API. Status code: {resp.StatusCode} and Body:{errorResponse}", null, resp.StatusCode);
        }
        var json = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<NewsApiResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    protected string BuildUrl()
    {
        return $"v2/everything?q={_query}&from={_fromDate}&sortBy={_sortBy}&apiKey={_apiKey}";
    }

    protected bool IsValidResponse(NewsApiResponse? response)
    {
        return response?.Articles != null;
    }

    protected ApiClientResult BuildApiClientResult(NewsApiResponse response)
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
