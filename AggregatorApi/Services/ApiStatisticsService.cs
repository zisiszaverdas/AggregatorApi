using AggregatorApi.Models;
using System.Collections.Concurrent;

namespace AggregatorApi.Services;

public class ApiStatisticsService : IApiStatisticsService
{
    private readonly ConcurrentDictionary<string, List<long>> _timings = new();
    private readonly ConcurrentDictionary<string, int> _failures = new();

    public void Record(string apiName, long elapsedMs)
    {
        _timings.AddOrUpdate(apiName, _ => new List<long> { elapsedMs }, (k, v) => { v.Add(elapsedMs); return v; });
    }

    public void RecordError(string apiName)
    {
        _failures.AddOrUpdate(apiName, 1, (k, v) => v + 1);
    }

    public IEnumerable<ApiStatisticsResult> GetAll()
    {
        var allKeys = _timings.Keys.Union(_failures.Keys);
        foreach (var api in allKeys)
        {
            var times = _timings.TryGetValue(api, out var t) ? t : new List<long>();
            var total = times.Count + (_failures.TryGetValue(api, out var f) ? f : 0);
            var avg = times.Count > 0 ? times.Average() : 0;
            var fast = times.Count(x => x < 100);
            var average = times.Count(x => x >= 100 && x <= 200);
            var slow = times.Count(x => x > 200);
            var failed = _failures.TryGetValue(api, out var failCount) ? failCount : 0;

            yield return new ApiStatisticsResult
            {
                ApiName = api,
                TotalRequests = total,
                FailedCount = failed,
                AverageResponseMs = avg,
                FastCount = fast,
                AverageCount = average,
                SlowCount = slow
            };
        }
    }
}
