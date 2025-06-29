namespace AggregatorApi.Services;

/// <summary>
/// Background service that periodically analyzes API performance statistics and logs anomalies.
/// </summary>
public class PerformanceAnalysisService : BackgroundService
{
    private readonly ILogger<PerformanceAnalysisService> logger;
    private readonly IApiStatisticsService statisticsService;
    private readonly ISystemClock clock;

    private static readonly TimeSpan _analysisInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan _window = TimeSpan.FromMinutes(5);
    private const int _minTotalSamples = 10;
    private const int _minWindowSamples = 5;
    private const double _anomalyThreshold = 1.5;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceAnalysisService"/> class.
    /// </summary>
    /// <param name="logger">The logger to use for logging anomalies and errors.</param>
    /// <param name="statisticsService">The statistics service to analyze.</param>
    /// <param name="clock">The system clock for time calculations.</param>
    public PerformanceAnalysisService(ILogger<PerformanceAnalysisService> logger, IApiStatisticsService statisticsService, ISystemClock clock)
    {
        this.logger = logger;
        this.statisticsService = statisticsService;
        this.clock = clock;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                AnalyzePerformance();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error analyzing performance statistics");
            }
            await Task.Delay(_analysisInterval, stoppingToken);
        }
    }

    /// <summary>
    /// Analyzes API performance statistics and logs a warning if a performance anomaly is detected.
    /// </summary>
    private void AnalyzePerformance()
    {
        var allStats = statisticsService.GetAll();
        foreach (var stat in allStats)
        {
            var timings = statisticsService.GetTimings(stat.ApiName);
            if (timings.Count < _minTotalSamples)
                continue;
            var now = clock.UtcNow;
            var windowTimings = timings.Where(x => (now - x.Timestamp) <= _window).ToList();
            if (windowTimings.Count < _minWindowSamples)
                continue;
            var windowAvg = windowTimings.Average(x => x.ElapsedMs);
            var overallAvg = timings.Average(x => x.ElapsedMs);
            if (overallAvg > 0 && windowAvg > overallAvg * _anomalyThreshold)
            {
                logger.LogWarning(
                    "Performance anomaly detected for {ApiName}: 5-min avg {WindowAvg}ms > 1.5x overall avg {OverallAvg}ms (total samples: {Total}, window samples: {Window})", 
                    stat.ApiName, windowAvg, overallAvg, timings.Count, windowTimings.Count);
            }
        }
    }
}
