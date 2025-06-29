using AggregatorApi.Models;
using Microsoft.Extensions.Caching.Hybrid;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Text.Json;

namespace AggregatorApi.Clients;

public abstract class ApiClientBase<TResponse> : IApiClient
{
    public abstract string SourceName { get; }
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly HybridCache _cache;
    private readonly string _cacheKey;
    private readonly HybridCacheEntryOptions _entryOptions;
    private readonly HybridCacheEntryOptions DefaultHybridCacheEntryOptions = new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(1) };

    protected virtual JsonSerializerOptions JsonOptions => new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    protected ApiClientBase(HttpClient httpClient, ILogger logger, HybridCache cache, string cacheKey, HybridCacheEntryOptions? entryOptions = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = cache;
        _cacheKey = cacheKey;
        _entryOptions = entryOptions ?? DefaultHybridCacheEntryOptions;
    }

    public async Task<ApiClientResult> FetchAsync(CancellationToken ct)
    {
        var url = BuildUrl();
        TResponse? response = default;
        try
        {
            response = await _cache.GetOrCreateAsync(
                _cacheKey,
                async (ct) => await FetchData(url, ct),
                _entryOptions);
        }
        catch (BrokenCircuitException ex)
        {
            return CreateErrorResult("Circuit breaker is open. External service is currently unavailable.", ex);
        }
        catch (TimeoutRejectedException ex)
        {
            return CreateErrorResult("Request timed out while fetching data from API.", ex);
        }
        catch (HttpRequestException ex)
        {
            return CreateErrorResult("Failed to fetch data from API.", ex);
        }
        catch (JsonException ex)
        {
            return CreateErrorResult("JSON deserialization error while processing API response.", ex);
        }
        catch (Exception ex)
        {
            return CreateErrorResult("Unexpected error while fetching data from API.", ex);
        }

        if (response == null)
        {
            return CreateErrorResult("Issue fetching data from API. Status code was not successful.");
        }

        if (!IsValidResponse(response))
        {
            return CreateErrorResult("Error response JSON contains empty properties or could not be fetched.");
        }
        return BuildApiClientResult(response);
    }

    protected virtual ApiClientResult CreateErrorResult(string message, Exception? ex = null)
    {
        var errorMessage = $"{SourceName}: {message}";
        return ApiClientResult.CreateErrorResult(errorMessage, _logger, ex);
    }

    protected virtual async Task<TResponse?> FetchData(string url, CancellationToken ct)
    {
        using var resp = await _httpClient.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var errorResponse = await resp.Content.ReadAsStringAsync();
            throw new HttpRequestException($"{SourceName}: Failed to fetch data from {SourceName} API. Status code: {resp.StatusCode} and Body:{errorResponse}", null, resp.StatusCode);
        }
        var json = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<TResponse>(json, JsonOptions);
    }

    protected virtual bool IsValidResponse(TResponse? response) => response != null;
    protected abstract string BuildUrl();
    protected abstract ApiClientResult BuildApiClientResult(TResponse response);
}
