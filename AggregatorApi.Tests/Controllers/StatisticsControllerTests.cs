using AggregatorApi.Controllers;
using AggregatorApi.Models;
using AggregatorApi.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace AggregatorApi.Tests.Controllers;

public class StatisticsControllerTests
{
    [Fact]
    public void Get_ReturnsStatisticsFromService()
    {
        var stats = new List<ApiStatisticsResult> { new ApiStatisticsResult { ApiName = "api", TotalRequests = 1 } };
        var service = Substitute.For<IApiStatisticsService>();
        service.GetAll().Returns(stats);
        var controller = new StatisticsController(service);

        var result = controller.Get();
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(stats, ok.Value);
    }
}
