namespace AggregatorApi.Models;

public class ApiStatisticsResult
{
    public string ApiName { get; set; } = default!;
    public int TotalRequests { get; set; }
    public int FailedCount { get; set; }
    public double AverageResponseMs { get; set; }
    public int FastCount { get; set; }
    public int AverageCount { get; set; }
    public int SlowCount { get; set; }
}