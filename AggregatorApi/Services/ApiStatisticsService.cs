using AggregatorApi.Models;
using System.Collections.Concurrent;

namespace AggregatorApi.Services;

public class ApiStatisticsService(ISystemClock clock) : IApiStatisticsService
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<ApiTimingEntry>> _timings = new();
    private readonly ConcurrentDictionary<string, int> _failures = new();

    public void Record(string apiName, long elapsedMs)
    {
        var entry = new ApiTimingEntry(elapsedMs, clock.UtcNow);
        var queue = _timings.GetOrAdd(apiName, _ => new ConcurrentQueue<ApiTimingEntry>());
        queue.Enqueue(entry);
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
            var times = _timings.TryGetValue(api, out var t) ? t : new ConcurrentQueue<ApiTimingEntry>();
            var timesList = times.ToList();
            var total = timesList.Count + (_failures.TryGetValue(api, out var f) ? f : 0);
            var avg = timesList.Count > 0 ? timesList.Average(x => x.ElapsedMs) : 0;
            var fast = timesList.Count(x => x.ElapsedMs < 100);
            var average = timesList.Count(x => x.ElapsedMs >= 100 && x.ElapsedMs <= 200);
            var slow = timesList.Count(x => x.ElapsedMs > 200);
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

    public IReadOnlyList<ApiTimingEntry> GetTimings(string apiName) =>
        _timings.TryGetValue(apiName, out var queue) ? queue.ToList() : new List<ApiTimingEntry>();
}