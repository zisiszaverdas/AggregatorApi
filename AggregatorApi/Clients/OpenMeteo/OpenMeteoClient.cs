using AggregatorApi.Models;
using System.Text.Json;

namespace AggregatorApi.Clients.OpenMeteo;

public class OpenMeteoClient(HttpClient HttpClient, ILogger<OpenMeteoClient> Logger) : IApiClient
{
    public const string ClientName = "OpenMeteo";
    public string SourceName => ClientName;
    private string forcastEndpoint => "v1/forecast";
    private const double AthensLatitude = 37.9838; // Athens, Greece
    private const double AthensLongitude = 23.7278; // Athens, Greece
    private const string parameterDateTimeFormat = "yyyy-MM-dd";

    public async Task<ApiClientResult> FetchAsync(CancellationToken ct)
    {
        var toDate = DateTime.Today;
        var fromDate = toDate.AddDays(-7);

        var url = $"{forcastEndpoint}?latitude={AthensLatitude}" +
            $"&longitude={AthensLongitude}" +
            $"&start_date={fromDate.ToString(parameterDateTimeFormat)}" +
            $"&end_date={toDate.ToString(parameterDateTimeFormat)}" +
            $"&daily=temperature_2m_max,temperature_2m_min";
        var json = string.Empty;
        try
        {
            using var resp = await HttpClient.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var errorMessage = $"{SourceName}: issue fetching data from OpenMeteo API. Status code: {resp.StatusCode}";
                return ApiClientResult.CreateErrorResult(errorMessage, Logger);
            }

            json = await resp.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            var errorMessage = $"{SourceName}: issue fetching data from OpenMeteo API. Exception: {ex.Message}";
            return ApiClientResult.CreateErrorResult(errorMessage, Logger, ex);
        }
       
        return ParseWeatherResponse(json);
       
    }


    private ApiClientResult ParseWeatherResponse(string json)
    {
        WeatherDailyResponse? weatherResponse;
        try
        {
            weatherResponse = JsonSerializer.Deserialize<WeatherDailyResponse>(json);
        }
        catch (Exception ex)
        {
            return ApiClientResult.CreateErrorResult($"{SourceName}: Error deserializing weather response JSON.", Logger, ex);
        }
        if (weatherResponse == null || weatherResponse.Daily == null)
        {
            return ApiClientResult.CreateErrorResult($"{SourceName}: Error weather response JSON contains empty properties.", Logger);
        }
        
        if ((weatherResponse?.Daily?.Time) == null || weatherResponse.Daily.Temperature2mMax == null || weatherResponse.Daily.Temperature2mMin == null)
        {
            return ApiClientResult.CreateErrorResult($"{SourceName}: Error weather response JSON contains empty properties.", Logger);
        }

        var aggregatedList = new List<AggregatedItem>();

        int minimumCount = new[] {
            weatherResponse.Daily.Time.Count,
            weatherResponse.Daily.Temperature2mMax.Count,
            weatherResponse.Daily.Temperature2mMin.Count
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
                    "Weather",
                    true));
        }

        return new ApiClientResult(aggregatedList);
    }

}
