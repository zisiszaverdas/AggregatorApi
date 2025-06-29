using System.Net;
using AggregatorApi.Middleware;
using AggregatorApi.Services;
using NSubstitute;

namespace AggregatorApi.Tests.Middleware;

public class StatisticsTrackingHandlerTests
{
    [Fact]
    public async Task SendAsync_RecordsTimingAndError()
    {
        var stats = Substitute.For<IApiStatisticsService>();
        var handler = new StatisticsTrackingHandler(stats, "api1")
        {
            InnerHandler = new FailingHandler()
        };
        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://test");

        // Should record error on exception
        await Assert.ThrowsAsync<HttpRequestException>(() => invoker.SendAsync(request, CancellationToken.None));
        stats.Received().RecordError("api1");

        // Should record timing on success
        handler = new StatisticsTrackingHandler(stats, "api2")
        {
            InnerHandler = new SuccessHandler()
        };
        invoker = new HttpMessageInvoker(handler);
        request = new HttpRequestMessage(HttpMethod.Get, "http://test");
        await invoker.SendAsync(request, CancellationToken.None);
        stats.Received().Record("api2", Arg.Any<long>());
    }

    private class FailingHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException();
    }
    private class SuccessHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
