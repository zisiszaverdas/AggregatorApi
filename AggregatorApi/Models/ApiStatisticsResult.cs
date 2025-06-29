namespace AggregatorApi.Models;

/// <summary>
/// Represents aggregated statistics for a specific API, including request counts and performance buckets.
/// </summary>
public class ApiStatisticsResult
{
    /// <summary>
    /// The name of the API.
    /// </summary>
    public string ApiName { get; set; } = default!;
    /// <summary>
    /// The total number of requests (successful + failed).
    /// </summary>
    public int TotalRequests { get; set; }
    /// <summary>
    /// The number of failed requests.
    /// </summary>
    public int FailedCount { get; set; }
    /// <summary>
    /// The average response time in milliseconds for successful requests.
    /// </summary>
    public double AverageResponseMs { get; set; }
    /// <summary>
    /// The number of requests considered fast (&lt;100ms).
    /// </summary>
    public int FastCount { get; set; }
    /// <summary>
    /// The number of requests considered average (100-200ms).
    /// </summary>
    public int AverageCount { get; set; }
    /// <summary>
    /// The number of requests considered slow (&gt;200ms).
    /// </summary>
    public int SlowCount { get; set; }
}