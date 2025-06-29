using AggregatorApi.Models;
using AggregatorApi.Services;
using Microsoft.Extensions.Caching.Hybrid;

namespace AggregatorApi.Clients.OpenMeteo;

public class OpenMeteoClient : ApiClientBase<WeatherDailyResponse>
{
    public const string ClientName = "OpenMeteo";
    public override string SourceName => ClientName;
    private string ForecastEndpoint => "v1/forecast";
    private const double AthensLatitude = 37.9838;
    private const double AthensLongitude = 23.7278;
    private const string parameterDateTimeFormat = "yyyy-MM-dd";
    private readonly DateTime FromDate;
    private readonly DateTime ToDate;
    private static readonly string CacheKey = $"OpenMeteoClient_Cache";

    public OpenMeteoClient(HttpClient httpClient, ILogger<OpenMeteoClient> logger, HybridCache cache, ISystemClock clock)
        : base(httpClient, logger, cache, CacheKey)
    {
        ToDate = clock.UtcNow.Date;
        FromDate = ToDate.AddDays(-7);
    }

    protected override string BuildUrl() => $"{ForecastEndpoint}?latitude={AthensLatitude}" +
               $"&longitude={AthensLongitude}" +
               $"&start_date={FromDate.ToString(parameterDateTimeFormat)}" +
               $"&end_date={ToDate.ToString(parameterDateTimeFormat)}" +
               $"&daily=temperature_2m_max,temperature_2m_min";

    protected override bool IsValidResponse(WeatherDailyResponse? response) => response != null &&
               response.Daily != null &&
               response.Daily.Time != null &&
               response.Daily.Temperature2mMax != null &&
               response.Daily.Temperature2mMin != null;

    protected override ApiClientResult BuildApiClientResult(WeatherDailyResponse weatherResponse)
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
                "Weather"
            ));
        }
        return new ApiClientResult(aggregatedList);
    }
}
