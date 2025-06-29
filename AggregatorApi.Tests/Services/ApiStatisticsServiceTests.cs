using AggregatorApi.Services;
using NSubstitute;

namespace AggregatorApi.Tests.Services;

public class ApiStatisticsServiceTests
{
    [Fact]
    public void Record_And_GetAll_TracksStatisticsCorrectly()
    {
        var clock = Substitute.For<ISystemClock>();
        var now = DateTime.UtcNow;
        clock.UtcNow.Returns(now, now.AddSeconds(1), now.AddSeconds(2), now.AddSeconds(3), now.AddSeconds(4), now.AddSeconds(5), now.AddSeconds(6));
        var service = new ApiStatisticsService(clock);
        service.Record("api1", 50);
        service.Record("api1", 150);
        service.Record("api1", 250);
        service.RecordError("api1");
        service.Record("api2", 80);
        service.RecordError("api2");
        service.RecordError("api2");

        var results = service.GetAll().ToList();
        var api1 = results.FirstOrDefault(x => x.ApiName == "api1");
        var api2 = results.FirstOrDefault(x => x.ApiName == "api2");

        Assert.NotNull(api1);
        Assert.Equal(4, api1.TotalRequests); // 3 timings + 1 failure
        Assert.Equal(1, api1.FailedCount);
        Assert.Equal((50 + 150 + 250) / 3.0, api1.AverageResponseMs);
        Assert.Equal(1, api1.FastCount); // <100
        Assert.Equal(1, api1.AverageCount); // 100-200
        Assert.Equal(1, api1.SlowCount); // >200

        Assert.NotNull(api2);
        Assert.Equal(3, api2.TotalRequests); // 1 timing + 2 failures
        Assert.Equal(2, api2.FailedCount);
        Assert.Equal(80, api2.AverageResponseMs);
        Assert.Equal(1, api2.FastCount);
        Assert.Equal(0, api2.AverageCount);
        Assert.Equal(0, api2.SlowCount);
    }

    [Fact]
    public void GetAll_ReturnsEmpty_WhenNoData()
    {
        var clock = Substitute.For<ISystemClock>();
        var service = new ApiStatisticsService(clock);
        var results = service.GetAll().ToList();
        Assert.Empty(results);
    }

    [Fact]
    public void GetAll_OnlyErrors()
    {
        var clock = Substitute.For<ISystemClock>();
        var service = new ApiStatisticsService(clock);
        service.RecordError("api1");
        service.RecordError("api1");
        var results = service.GetAll().ToList();
        Assert.Single(results);
        Assert.Equal(2, results[0].TotalRequests);
        Assert.Equal(2, results[0].FailedCount);
        Assert.Equal(0, results[0].AverageResponseMs);
    }

    [Fact]
    public void GetAll_OnlyTimings()
    {
        var clock = Substitute.For<ISystemClock>();
        var now = DateTime.UtcNow;
        clock.UtcNow.Returns(now, now.AddSeconds(1));
        var service = new ApiStatisticsService(clock);
        service.Record("api1", 100);
        service.Record("api1", 200);
        var results = service.GetAll().ToList();
        Assert.Single(results);
        Assert.Equal(2, results[0].TotalRequests);
        Assert.Equal(0, results[0].FailedCount);
        Assert.Equal(150, results[0].AverageResponseMs);
    }

    [Fact]
    public async Task Record_And_GetAll_Concurrent()
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(DateTime.UtcNow);
        var service = new ApiStatisticsService(clock);
        var tasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            int idx = i;
            tasks.Add(Task.Run(() => service.Record("api1", idx)));
            if (i % 10 == 0)
                tasks.Add(Task.Run(() => service.RecordError("api1")));
        }
        await Task.WhenAll(tasks);
        var result = service.GetAll().FirstOrDefault(x => x.ApiName == "api1");
        Assert.NotNull(result);
        Assert.Equal(110, result.TotalRequests); // 100 timings + 10 errors
        Assert.Equal(10, result.FailedCount);
        Assert.Equal(99 / 2.0, result.AverageResponseMs, 0); // average of 0..99
    }
}
