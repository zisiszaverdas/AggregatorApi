using System.Net;
using AggregatorApi.Clients.NewsApi;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;

namespace AggregatorApi.Tests.Clients;

public class NewsApiClientTests
{
    private NewsApiClient CreateClient(MockHttpMessageHandler mockHttp, string? apiKey = "test-key")
    {
        var httpClient = new HttpClient(mockHttp)
        {
            BaseAddress = new Uri("https://newsapi.org/")
        };
        var logger = NSubstitute.Substitute.For<ILogger<NewsApiClient>>();
        var services = new ServiceCollection();
        services.AddHybridCache();
        var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("NewsApi:ApiKey", apiKey) })
            .Build();
        return new NewsApiClient(httpClient, logger, cache, config);
    }

    [Fact]
    public void SourceName_IsNewsApi()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("*").Respond(HttpStatusCode.BadRequest);
        var client = CreateClient(mockHttp);
        Assert.Equal("NewsApi", client.SourceName);
    }

    [Fact]
    public async Task FetchAsync_ReturnsAggregatedItems_OnSuccess()
    {
        var json = @"{
    ""articles"": [
        { ""title"": ""T1"", ""description"": ""D1"", ""publishedAt"": ""2024-01-01T00:00:00Z"" },
        { ""title"": ""T2"", ""description"": ""D2"", ""publishedAt"": ""2024-01-02T00:00:00Z"" }
    ]
}";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("*").Respond("application/json", json);
        var client = CreateClient(mockHttp);

        var result = await client.FetchAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Contains(items, x => x.Title == "T1");
        Assert.Contains(items, x => x.Title == "T2");
        Assert.All(items, x => Assert.Equal("NewsApi", x.Source));
        Assert.All(items, x => Assert.Equal("News", x.Category));
    }

    [Fact]
    public async Task FetchAsync_ReturnsError_WhenArticlesNull()
    {
        var json = @"{ ""articles"": null }";
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
