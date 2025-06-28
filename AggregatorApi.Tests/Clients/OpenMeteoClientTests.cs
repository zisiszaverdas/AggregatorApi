using System.Net;
using AggregatorApi.Clients.OpenMeteo;
using RichardSzalay.MockHttp;

namespace AggregatorApi.Tests.Clients;

public class OpenMeteoClientTests
{
    // Helper to create a client with BaseAddress set
    private static OpenMeteoClient CreateClientWithBase(MockHttpMessageHandler mockHttp)
    {
        var httpClient = new HttpClient(mockHttp)
        {
            BaseAddress = new Uri("https://api.open-meteo.com/")
        };
        return new OpenMeteoClient(httpClient);
    }

    [Fact]
    public void SourceName_IsOpenMeteo()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("*").Respond(HttpStatusCode.BadRequest);
        var client = CreateClientWithBase(mockHttp);

        // Act & Assert
        Assert.Equal("OpenMeteo", client.SourceName);
    }

    [Fact]
    public async Task FetchAsync_ReturnsEmpty_WhenResponseIsNotSuccess()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("*").Respond(HttpStatusCode.BadRequest);
        var client = CreateClientWithBase(mockHttp);

        // Act
        var result = await client.FetchAsync(CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task FetchAsync_ReturnsEmpty_WhenDailyIsNull()
    {
        // Arrange
        var json = "{\"Daily\":null}";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("*").Respond("application/json", json);
        var client = CreateClientWithBase(mockHttp);

        // Act
        var result = await client.FetchAsync(CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task FetchAsync_ReturnsEmpty_WhenDailyPropertiesAreNull()
    {
        // Arrange
        var json = @"{
            ""daily"": {
                ""time"": null,
                ""temperature_2m_max"": null,
                ""temperature_2m_min"": null
            }
        }";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("*").Respond("application/json", json);
        var client = CreateClientWithBase(mockHttp);

        // Act
        var result = await client.FetchAsync(CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task FetchAsync_ReturnsAggregatedItems_WhenValidResponse()
    {
        // Arrange
        var json = @"{
            ""daily"": {
                ""time"": [""2024-06-01"", ""2024-06-02""],
                ""temperature_2m_max"": [30, 32],
                ""temperature_2m_min"": [20, 21]
            },
            ""daily_units"": {
                ""temperature_2m_max"": ""°C"",
                ""temperature_2m_min"": ""°C""
            }
        }";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("*").Respond("application/json", json);
        var client = CreateClientWithBase(mockHttp);

        // Act
        var result = (await client.FetchAsync(CancellationToken.None)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, x => Assert.Equal("OpenMeteo", x.Source));
        Assert.Equal("Weather Forecast for 2024-06-01", result[0].Title);
        Assert.Contains("Max Temp: 30°C, Min Temp: 20°C", result[0].Description);
        Assert.Equal("Weather", result[0].Category);
    }

  

    [Fact]
    public async Task FetchAsync_ReturnsEmpty_WhenResponseBodyIsInvalid()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("*").Respond("application/json", "invalid json");
        var client = CreateClientWithBase(mockHttp);

        // Act
        var result = await client.FetchAsync(CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }
}