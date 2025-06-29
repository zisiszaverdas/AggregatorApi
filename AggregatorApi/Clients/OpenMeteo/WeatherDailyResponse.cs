using System.Text.Json.Serialization;

namespace AggregatorApi.Clients.OpenMeteo;

public class WeatherDailyResponse
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }

    [JsonPropertyName("timezone_abbreviation")]
    public string? TimezoneAbbreviation { get; set; }

    [JsonPropertyName("elevation")]
    public double Elevation { get; set; }

    [JsonPropertyName("daily_units")]
    public DailyUnits? DailyUnits { get; set; }

    [JsonPropertyName("daily")]
    public DailyData? Daily { get; set; }
}

public class DailyUnits
{
    [JsonPropertyName("time")]
    public string? Time { get; set; }

    [JsonPropertyName("temperature_2m_max")]
    public string? Temperature2mMax { get; set; }

    [JsonPropertyName("temperature_2m_min")]
    public string? Temperature2mMin { get; set; }
}

public class DailyData
{
    [JsonPropertyName("time")]
    public List<string>? Time { get; set; }

    [JsonPropertyName("temperature_2m_max")]
    public List<double>? Temperature2mMax { get; set; }

    [JsonPropertyName("temperature_2m_min")]
    public List<double>? Temperature2mMin { get; set; }
}
