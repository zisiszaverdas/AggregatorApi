using System.Net;
using AggregatorApi.Clients;
using AggregatorApi.Models;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RichardSzalay.MockHttp;

namespace AggregatorApi.Tests.Clients;

public class ApiClientBaseTests
{
    private class TestApiClient : ApiClientBase<string>
    {
        public override string SourceName => "TestSource";
        private readonly string _url;
        private readonly bool _valid;
        public TestApiClient(HttpClient httpClient, ILogger logger, HybridCache cache, string url, bool valid = true)
            : base(httpClient, logger, cache, "TestCacheKey") { _url = url; _valid = valid; }
        protected override string BuildUrl() => _url;
        protected override bool IsValidResponse(string? response) => _valid && response == "ok";
        protected override ApiClientResult BuildApiClientResult(string response) => new([new AggregatedItem("Test", "T", "D", DateTime.Now, "C")]);
    }

    private class ExceptionThrowingApiClient : ApiClientBase<string>
    {
        private readonly Exception _exceptionToThrow;
        public override string SourceName => "TestSource";
        public ExceptionThrowingApiClient(HttpClient httpClient, ILogger logger, HybridCache cache, string url, Exception exception)
            : base(httpClient, logger, cache, "TestCacheKey")
        {
            _exceptionToThrow = exception;
        }
        protected override string BuildUrl() => "test-url";
        protected override bool IsValidResponse(string? response) => true;
        protected override ApiClientResult BuildApiClientResult(string response) => new([new AggregatedItem("Test", "T", "D", DateTime.Now, "C")]);
        protected override Task<string?> FetchData(string url, CancellationToken ct) => throw _exceptionToThrow;
    }

    private TestApiClient CreateClient(MockHttpMessageHandler mockHttp, bool valid = true)
    {
        var httpClient = new HttpClient(mockHttp) { BaseAddress = new Uri("https://test/") };
        var logger = Substitute.For<ILogger>();
        var services = new ServiceCollection();
        services.AddHybridCache();
        var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();
        return new TestApiClient(httpClient, logger, cache, "test-url", valid);
    }

    [Fact]
    public async Task FetchAsync_ReturnsResult_OnValidResponse()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("*").Respond("application/json", "\"ok\"");
        var client = CreateClient(mockHttp);

        var result = await client.FetchAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.Single(result.Items);
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

    [Fact]
    public async Task FetchAsync_ReturnsError_OnInvalidResponse()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("*").Respond("application/json", "\"not-ok\"");
        var client = CreateClient(mockHttp, valid: false);

        var result = await client.FetchAsync(CancellationToken.None);

        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("empty properties", result.ErrorMessage);
    }

    [Fact]
    public async Task FetchAsync_LogsAndReturnsError_OnUnexpectedException()
    {
        var mockHttp = new MockHttpMessageHandler();
        // Simulate an unexpected exception by disposing the handler before use
        var client = CreateClient(mockHttp);
        // Dispose the HttpClient's handler to force an ObjectDisposedException
        mockHttp.Dispose();
        var result = await client.FetchAsync(CancellationToken.None);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Unexpected error while fetching data from API", result.ErrorMessage);
    }

    [Fact]
    public async Task FetchAsync_LogsAndReturnsError_OnTimeoutRejectedException()
    {
        var logger = Substitute.For<ILogger>();
        var services = new ServiceCollection();
        services.AddHybridCache();
        var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();
        var httpClient = new HttpClient(new MockHttpMessageHandler()) { BaseAddress = new Uri("https://test/") };
        var client = new ExceptionThrowingApiClient(httpClient, logger, cache, "test-url", new Polly.Timeout.TimeoutRejectedException());
        var result = await client.FetchAsync(CancellationToken.None);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Request timed out while fetching data from API", result.ErrorMessage);
    }

    [Fact]
    public async Task FetchAsync_LogsAndReturnsError_OnBrokenCircuitException()
    {
        var logger = Substitute.For<ILogger>();
        var services = new ServiceCollection();
        services.AddHybridCache();
        var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();
        var httpClient = new HttpClient(new MockHttpMessageHandler()) { BaseAddress = new Uri("https://test/") };
        var client = new ExceptionThrowingApiClient(httpClient, logger, cache, "test-url", new Polly.CircuitBreaker.BrokenCircuitException());
        var result = await client.FetchAsync(CancellationToken.None);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Circuit breaker is open. External service is currently unavailable.", result.ErrorMessage);
    }
}
