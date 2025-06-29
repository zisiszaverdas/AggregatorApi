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
}
