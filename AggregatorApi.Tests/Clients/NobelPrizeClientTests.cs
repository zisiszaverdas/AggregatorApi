using System.Net;
using AggregatorApi.Clients.NobelPrize;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;

namespace AggregatorApi.Tests.Clients;

public class NobelPrizeClientTests
{
    private NobelPrizeClient CreateClient(MockHttpMessageHandler mockHttp)
    {
        var httpClient = new HttpClient(mockHttp)
        {
            BaseAddress = new Uri("http://api.nobelprize.org/")
        };
        var logger = Substitute.For<ILogger<NobelPrizeClient>>();
        var services = new ServiceCollection();
        services.AddHybridCache();
        var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();
        return new NobelPrizeClient(httpClient, logger, cache);
    }

    [Fact]
    public void SourceName_IsNobelPrize()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("*").Respond(HttpStatusCode.BadRequest);
        var client = CreateClient(mockHttp);
        Assert.Equal("NobelPrize", client.SourceName);
    }

    [Fact]
    public async Task FetchAsync_ReturnsAggregatedItems_OnSuccess()
    {
        var json = @"{
          ""nobelPrizes"": [
            {
              ""awardYear"": ""2024"",
              ""category"": { ""en"": ""Chemistry"" },
              ""categoryFullName"": { ""en"": ""The Nobel Prize in Chemistry"" },
              ""dateAwarded"": ""2024-10-09"",
              ""laureates"": [
                {
                  ""id"": ""1039"",
                  ""knownName"": { ""en"": ""David Baker"" },
                  ""fullName"": { ""en"": ""David Baker"" },
                  ""motivation"": { ""en"": ""for computational protein design"" }
                }
              ]
            }
          ]
        }";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("*").Respond("application/json", json);
        var client = CreateClient(mockHttp);

        var result = await client.FetchAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        var items = result.Items.ToList();
        Assert.Single(items);
        Assert.Equal("NobelPrize", items[0].Source);
        Assert.Contains("David Baker", items[0].Title);
        Assert.Contains("Chemistry", items[0].Title);
        Assert.Contains("computational protein design", items[0].Description);
        Assert.Equal("NobelPrize", items[0].Category);
    }

    [Fact]
    public async Task FetchAsync_ReturnsError_WhenNobelPrizesNull()
    {
        var json = @"{ ""nobelPrizes"": null }";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("*").Respond("application/json", json);
        var client = CreateClient(mockHttp);

        var result = await client.FetchAsync(CancellationToken.None);

        Assert.NotNull(result.ErrorMessage);
        Assert.Empty(result.Items);
        Assert.Contains("empty properties", result.ErrorMessage);
    }

    [Fact]
    public async Task FetchAsync_ReturnsError_OnHttpError()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("*").Respond(HttpStatusCode.BadRequest, "application/json", "error");
        var client = CreateClient(mockHttp);

        var result = await client.FetchAsync(CancellationToken.None);

        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Failed to fetch data", result.ErrorMessage);
    }

    [Fact]
    public async Task FetchAsync_ReturnsError_OnJsonError()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("*").Respond("application/json", "not-json");
        var client = CreateClient(mockHttp);

        var result = await client.FetchAsync(CancellationToken.None);

        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("JSON deserialization error", result.ErrorMessage);
    }
}
