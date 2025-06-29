using AggregatorApi.Models;
using Microsoft.Extensions.Caching.Hybrid;
using System.Text.Json;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace AggregatorApi.Clients.OpenMeteo;

public class OpenMeteoClient(HttpClient httpClient, ILogger<OpenMeteoClient> logger, HybridCache cache) : IApiClient
{
    public const string ClientName = "OpenMeteo";
    public string SourceName => ClientName;
    private string ForecastEndpoint => "v1/forecast";
    private const double AthensLatitude = 37.9838;
    private const double AthensLongitude = 23.7278;
    private const string parameterDateTimeFormat = "yyyy-MM-dd";
    private static readonly string CacheKey = $"OpenMeteoClient_Cache";
    private static readonly HybridCacheEntryOptions EntryOptions = new() { Expiration = TimeSpan.FromMinutes(1) };

    public async Task<ApiClientResult> FetchAsync(CancellationToken ct)
    {
        var url = BuildForecastUrl();
        WeatherDailyResponse? weatherResponse = null;
        try
        {
            weatherResponse = await cache.GetOrCreateAsync(
                CacheKey,
                async (ct) => await FetchData(url, ct),
                EntryOptions);
        }
        catch (BrokenCircuitException ex)
        {
            var errorMessage = $"{SourceName}: Circuit breaker is open. External service is currently unavailable.";
            return ApiClientResult.CreateErrorResult(errorMessage, logger, ex);
        }
        catch (TimeoutRejectedException ex)
        {
            var errorMessage = $"{SourceName}: Request timed out while fetching data from {SourceName} API.";
            return ApiClientResult.CreateErrorResult(errorMessage, logger, ex);
        }
        catch (HttpRequestException ex)
        {
            var errorMessage = $"{SourceName}: Failed to fetch data from {SourceName} API.";
            return ApiClientResult.CreateErrorResult(errorMessage, logger, ex);
        }
        catch (JsonException ex)
        {
            var errorMessage = $"{SourceName}: JSON deserialization error while processing {SourceName} API response.";
            return ApiClientResult.CreateErrorResult(errorMessage, logger, ex);
        }
        catch (Exception ex)
        {
            var errorMessage = $"{SourceName}: Unexpected error while fetching data from {SourceName} API.";
            return ApiClientResult.CreateErrorResult(errorMessage, logger, ex);
        }

        if (weatherResponse == null)
        {
            var errorMessage = $"{SourceName}: issue fetching data from {SourceName} API. Status code was not successful.";
            return ApiClientResult.CreateErrorResult(errorMessage, logger);
        }

        if (!IsValidWeatherResponse(weatherResponse))
        {
            return ApiClientResult.CreateErrorResult($"{SourceName}: Error response JSON contains empty properties or could not be fetched.", logger);
        }
        return BuildApiClientResult(weatherResponse);
    }

    private async Task<WeatherDailyResponse?> FetchData(string url, CancellationToken ct)
    {
        using var resp = await httpClient.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"{SourceName}: Failed to fetch data from {SourceName} API. Status code: {resp.StatusCode}", null ,resp.StatusCode);
        }
        var json = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<WeatherDailyResponse>(json);
    }

    private string BuildForecastUrl()
    {
        var toDate = DateTime.Today;
        var fromDate = toDate.AddDays(-7);
        return $"{ForecastEndpoint}?latitude={AthensLatitude}" +
               $"&longitude={AthensLongitude}" +
               $"&start_date={fromDate.ToString(parameterDateTimeFormat)}" +
               $"&end_date={toDate.ToString(parameterDateTimeFormat)}" +
               $"&daily=temperature_2m_max,temperature_2m_min";
    }

    private static bool IsValidWeatherResponse(WeatherDailyResponse? response)
    {
        return response != null &&
               response.Daily != null &&
               response.Daily.Time != null &&
               response.Daily.Temperature2mMax != null &&
               response.Daily.Temperature2mMin != null;
    }

    private ApiClientResult BuildApiClientResult(WeatherDailyResponse weatherResponse)
    {
        var aggregatedList = new List<AggregatedItem>();
        int minimumCount = new[]
        {
            weatherResponse.Daily!.Time!.Count,
            weatherResponse.Daily.Temperature2mMax!.Count,
            weatherResponse.Daily.Temperature2mMin!.Count
        }.Min();
        for (int i = 0; i < minimumCount; i++)
        {
            var day = weatherResponse.Daily.Time[i];
            var max = weatherResponse.Daily.Temperature2mMax[i];
            var min = weatherResponse.Daily.Temperature2mMin[i];
            aggregatedList.Add(new AggregatedItem(
                SourceName,
                $"Weather Forecast for {day}",
                $"Max Temp: {max}{weatherResponse.DailyUnits?.Temperature2mMax}, Min Temp: {min}{weatherResponse.DailyUnits?.Temperature2mMin}",
                DateTime.Parse(day),
                "Weather"));
        }
        return new ApiClientResult(aggregatedList);
    }
}
