using AggregatorApi.Models;

namespace AggregatorApi.Services;

/// <summary>
/// Provides methods to record and retrieve API request statistics, including timings and error counts.
/// </summary>
public interface IApiStatisticsService
{
    /// <summary>
    /// Records a successful API call timing.
    /// </summary>
    /// <param name="apiName">The name of the API.</param>
    /// <param name="elapsedMs">The elapsed time in milliseconds.</param>
    void Record(string apiName, long elapsedMs);

    /// <summary>
    /// Records a failed API call.
    /// </summary>
    /// <param name="apiName">The name of the API.</param>
    void RecordError(string apiName);

    /// <summary>
    /// Gets statistics for all tracked APIs, including total requests, failures, and performance buckets.
    /// </summary>
    /// <returns>A collection of statistics results for each API.</returns>
    IEnumerable<ApiStatisticsResult> GetAll();

    /// <summary>
    /// Gets the timing entries for a specific API.
    /// </summary>
    /// <param name="apiName">The name of the API.</param>
    /// <returns>A read-only list of timing entries.</returns>
    IReadOnlyList<ApiTimingEntry> GetTimings(string apiName);
}