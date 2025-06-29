using System.Net;
using AggregatorApi.Clients.OpenMeteo;
using Microsoft.Extensions.Logging;
using NSubstitute;
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
        var logger = Substitute.For<ILogger<OpenMeteoClient>>();
        return new OpenMeteoClient(httpClient, logger);
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
    public async Task FetchAsync_ReturnsError_WhenResponseIsNotSuccess()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("*").Respond(HttpStatusCode.BadRequest);
        var client = CreateClientWithBase(mockHttp);

        // Act
        var result = await client.FetchAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Status code", result.ErrorMessage);
    }

    [Fact]
    public async Task FetchAsync_ReturnsError_WhenDailyIsNull()
    {
        // Arrange
        var json = "{\"daily\":null}";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("*").Respond("application/json", json);
        var client = CreateClientWithBase(mockHttp);

        // Act
        var result = await client.FetchAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("empty properties", result.ErrorMessage);
    }

    [Fact]
    public async Task FetchAsync_ReturnsError_WhenDailyPropertiesAreNull()
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
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("empty properties", result.ErrorMessage);
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
        var result = await client.FetchAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.All(items, x => Assert.Equal("OpenMeteo", x.Source));
        Assert.Equal("Weather Forecast for 2024-06-01", items[0].Title);
        Assert.Contains("Max Temp: 30°C, Min Temp: 20°C", items[0].Description);
        Assert.Equal("Weather", items[0].Category);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task FetchAsync_ReturnsError_WhenResponseBodyIsInvalid()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("*").Respond("application/json", "invalid json");
        var client = CreateClientWithBase(mockHttp);

        // Act
        var result = await client.FetchAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("deserializing", result.ErrorMessage);
    }
}