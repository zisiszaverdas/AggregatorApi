using AggregatorApi.Clients.OpenMeteo;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;
using System.Net;

namespace AggregatorApi.Tests.Clients;

public class OpenMeteoClientTests
{
    // Helper to create a client with BaseAddress set
    private OpenMeteoClient CreateClientWithBase(MockHttpMessageHandler mockHttp)
    {
        var httpClient = new HttpClient(mockHttp)
        {
            BaseAddress = new Uri("https://api.open-meteo.com/")
        };
        var logger = NSubstitute.Substitute.For<ILogger<OpenMeteoClient>>();
        var services = new ServiceCollection();
        services.AddHybridCache();
        var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();
        return new OpenMeteoClient(httpClient, logger, cache);
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
        Assert.Contains("OpenMeteo: Failed to fetch data from OpenMeteo API. Status code: BadRequest", result.ErrorMessage);
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
        Assert.Contains("OpenMeteo: JSON deserialization error while processing OpenMeteo API response.", result.ErrorMessage);
    }
}