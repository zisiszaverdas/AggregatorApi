using AggregatorApi.Models;

namespace AggregatorApi.Services;

public interface IApiStatisticsService
{
    void Record(string apiName, long elapsedMs);
    void RecordError(string apiName);
    IEnumerable<ApiStatisticsResult> GetAll();
    IReadOnlyList<ApiTimingEntry> GetTimings(string apiName);
}