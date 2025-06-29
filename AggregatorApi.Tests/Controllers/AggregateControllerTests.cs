using AggregatorApi.Controllers;
using AggregatorApi.Models;
using AggregatorApi.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace AggregatorApi.Tests.Controllers;

public class AggregateControllerTests
{
    [Fact]
    public async Task Get_ReturnsOkResult_WithAggregatedItems()
    {
        // Arrange
        var expectedItems = new List<AggregatedItem>
        {
            new("Source1", "Title1", "Desc1", System.DateTime.Today, "Weather", true),
            new("Source2", "Title2", "Desc2", System.DateTime.Today, "News", true)
        };

        var aggregationService = Substitute.For<IAggregationService>();
        var request = new AggregationRequest { Category = "Weather" };
        aggregationService.GetAggregatedDataAsync(request, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AggregatorResponse(expectedItems)));

        var controller = new AggregateController(aggregationService);

        // Act
        var result = await controller.Get(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<AggregatorResponse>(okResult.Value);
        Assert.Equal(expectedItems, response.Items);
    }

    [Fact]
    public async Task Get_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var aggregationService = Substitute.For<IAggregationService>();
        var request = new AggregationRequest
        {
            Date = System.DateTime.Today,
            Category = "News",
            SortBy = "date",
            Descending = true
        };

        aggregationService.GetAggregatedDataAsync(request, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AggregatorResponse(new List<AggregatedItem>())));

        var controller = new AggregateController(aggregationService);

        // Act
        await controller.Get(request, CancellationToken.None);

        // Assert
        await aggregationService.Received(1).GetAggregatedDataAsync(request, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Get_ReturnsOkResult_EvenWhenNoItems()
    {
        // Arrange
        var aggregationService = Substitute.For<IAggregationService>();
        var request = new AggregationRequest();
        aggregationService.GetAggregatedDataAsync(request, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AggregatorResponse(new List<AggregatedItem>())));

        var controller = new AggregateController(aggregationService);

        // Act
        var result = await controller.Get(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<AggregatorResponse>(okResult.Value);
        Assert.Empty(response.Items);
    }
}