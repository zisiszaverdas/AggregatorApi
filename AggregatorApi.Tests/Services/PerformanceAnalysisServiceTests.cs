using AggregatorApi.Models;
using AggregatorApi.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.Core;

namespace AggregatorApi.Tests.Services;

public class PerformanceAnalysisServiceTests
{
    private static bool LogWasCalledWithWarning(IEnumerable<ICall> calls)
    {
        return calls.Any(call =>
            call.GetArguments().Length > 0 &&
            call.GetArguments()[0] is LogLevel level &&
            level == LogLevel.Warning
        );
    }

    [Fact]
    public void NoAnomaly_DoesNotLogWarning()
    {
        var logger = Substitute.For<ILogger<PerformanceAnalysisService>>();
        var stats = Substitute.For<IApiStatisticsService>();
        var clock = Substitute.For<ISystemClock>();
        var apiName = "api1";
        var now = DateTime.UtcNow;
        clock.UtcNow.Returns(now);
        var timings = Enumerable.Range(0, 20)
            .Select(i => new ApiTimingEntry(100, now.AddSeconds(-i * 10)))
            .ToList();
        stats.GetAll().Returns(new[] { new ApiStatisticsResult { ApiName = apiName } });
        stats.GetTimings(apiName).Returns(timings);
        var service = new PerformanceAnalysisService(logger, stats, clock);
        var method = typeof(PerformanceAnalysisService).GetMethod("AnalyzePerformance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(service, null);
        var calls = logger.ReceivedCalls().ToList();
        Assert.False(LogWasCalledWithWarning(calls));
    }

    [Fact]
    public void Anomaly_LogsWarning()
    {
        var logger = Substitute.For<ILogger<PerformanceAnalysisService>>();
        var stats = Substitute.For<IApiStatisticsService>();
        var clock = Substitute.For<ISystemClock>();
        var apiName = "api1";
        var now = DateTime.UtcNow;
        clock.UtcNow.Returns(now);
        // Ensure the last 10 entries (within 5 min window) are 400ms, older 10 are 100ms
        var timings = Enumerable.Range(0, 10)
            .Select(i => new ApiTimingEntry(100, now.AddMinutes(-6).AddSeconds(-i))) // outside window
            .Concat(Enumerable.Range(0, 10).Select(i => new ApiTimingEntry(400, now.AddSeconds(-i * 30)))) // within window
            .ToList();
        stats.GetAll().Returns(new[] { new ApiStatisticsResult { ApiName = apiName } });
        stats.GetTimings(apiName).Returns(timings);
        var service = new PerformanceAnalysisService(logger, stats, clock);
        var method = typeof(PerformanceAnalysisService).GetMethod("AnalyzePerformance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(service, null);
        var calls = logger.ReceivedCalls().ToList();
        Assert.True(LogWasCalledWithWarning(calls));
    }

    [Fact]
    public void NoApis_DoesNotLogAnything()
    {
        var logger = Substitute.For<ILogger<PerformanceAnalysisService>>();
        var stats = Substitute.For<IApiStatisticsService>();
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(DateTime.UtcNow);
        stats.GetAll().Returns(Array.Empty<ApiStatisticsResult>());
        var service = new PerformanceAnalysisService(logger, stats, clock);
        var method = typeof(PerformanceAnalysisService).GetMethod("AnalyzePerformance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(service, null);
        var calls = logger.ReceivedCalls().ToList();
        Assert.Empty(calls);
    }

    [Fact]
    public void NotEnoughTotalSamples_DoesNotLogWarning()
    {
        var logger = Substitute.For<ILogger<PerformanceAnalysisService>>();
        var stats = Substitute.For<IApiStatisticsService>();
        var clock = Substitute.For<ISystemClock>();
        var apiName = "api1";
        var now = DateTime.UtcNow;
        clock.UtcNow.Returns(now);
        var timings = Enumerable.Range(0, 5)
            .Select(i => new ApiTimingEntry(100, now.AddSeconds(-i * 10)))
            .ToList();
        stats.GetAll().Returns(new[] { new ApiStatisticsResult { ApiName = apiName } });
        stats.GetTimings(apiName).Returns(timings);
        var service = new PerformanceAnalysisService(logger, stats, clock);
        var method = typeof(PerformanceAnalysisService).GetMethod("AnalyzePerformance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(service, null);
        var calls = logger.ReceivedCalls().ToList();
        Assert.Empty(calls);
    }

    [Fact]
    public void NotEnoughWindowSamples_DoesNotLogWarning()
    {
        var logger = Substitute.For<ILogger<PerformanceAnalysisService>>();
        var stats = Substitute.For<IApiStatisticsService>();
        var clock = Substitute.For<ISystemClock>();
        var apiName = "api1";
        var now = DateTime.UtcNow;
        clock.UtcNow.Returns(now);
        var timings = Enumerable.Range(0, 15)
            .Select(i => new ApiTimingEntry(100, now.AddMinutes(-10 - i))) // all outside window
            .Concat(Enumerable.Range(0, 3).Select(i => new ApiTimingEntry(400, now.AddSeconds(-i * 30)))) // only 3 in window
            .ToList();
        stats.GetAll().Returns(new[] { new ApiStatisticsResult { ApiName = apiName } });
        stats.GetTimings(apiName).Returns(timings);
        var service = new PerformanceAnalysisService(logger, stats, clock);
        var method = typeof(PerformanceAnalysisService).GetMethod("AnalyzePerformance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(service, null);
        var calls = logger.ReceivedCalls().ToList();
        Assert.Empty(calls);
    }

    [Fact]
    public async Task AnalyzePerformance_Exception_LogsError()
    {
        var logger = Substitute.For<ILogger<PerformanceAnalysisService>>();
        var stats = Substitute.For<IApiStatisticsService>();
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(DateTime.UtcNow);
        stats.GetAll().Returns(_ => throw new InvalidOperationException("fail"));
        var service = new PerformanceAnalysisService(logger, stats, clock);
        var token = new CancellationTokenSource();
        token.CancelAfter(100); // stop after first loop
        await service.StartAsync(token.Token);
        logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Error analyzing performance statistics")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());
    }
}
