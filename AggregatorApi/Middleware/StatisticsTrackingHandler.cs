using System.Diagnostics;
using AggregatorApi.Services;

namespace AggregatorApi.Middleware;

public class StatisticsTrackingHandler(IApiStatisticsService ApiStatisticsService, string ApiName) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var response = await base.SendAsync(request, cancellationToken);
            ApiStatisticsService.Record(ApiName, sw.ElapsedMilliseconds);
            return response;
        }
        catch
        {
            ApiStatisticsService.RecordError(ApiName);
            throw;
        }
    }
}