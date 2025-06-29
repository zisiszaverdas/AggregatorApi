using AggregatorApi.Models;
using AggregatorApi.Services;
using AggregatorApi.Clients;
using NSubstitute;

namespace AggregatorApi.Tests.Services;

public class AggregationServiceTests
{
    private static AggregatedItem CreateItem(
        string source = "TestSource",
        string title = "TestTitle",
        string description = "TestDescription",
        DateTime? date = null,
        string category = "TestCategory")
    {
        return new AggregatedItem(
            source,
            title,
            description,
            date ?? DateTime.Today,
            category
        );
    }

    private static ApiClientResult CreateApiClientResult(params AggregatedItem[] items)
        => new ApiClientResult(items);

    [Fact]
    public async Task GetAggregatedDataAsync_ReturnsAggregatedItemsFromAllClients()
    {
        // Arrange
        var items1 = new[] { CreateItem(title: "Item1") };
        var items2 = new[] { CreateItem(title: "Item2") };

        var client1 = Substitute.For<IApiClient>();
        client1.FetchAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateApiClientResult(items1)));

        var client2 = Substitute.For<IApiClient>();
        client2.FetchAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateApiClientResult(items2)));

        var service = new AggregationService(new[] { client1, client2 });
        var request = new AggregationRequest();

        // Act
        var response = await service.GetAggregatedDataAsync(request, CancellationToken.None);
        var result = response.Items.ToList();

        // Assert
        Assert.Contains(result, x => x.Title == "Item1");
        Assert.Contains(result, x => x.Title == "Item2");
    }

    [Fact]
    public async Task GetAggregatedDataAsync_FiltersByCategory()
    {
        // Arrange
        var items = new[]
        {
            CreateItem(category: "Weather"),
            CreateItem(category: "News")
        };

        var client = Substitute.For<IApiClient>();
        client.FetchAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateApiClientResult(items)));

        var service = new AggregationService(new[] { client });
        var request = new AggregationRequest { Category = "Weather" };

        // Act
        var response = await service.GetAggregatedDataAsync(request, CancellationToken.None);
        var result = response.Items.ToList();

        // Assert
        Assert.Single(result);
        Assert.All(result, x => Assert.Equal("Weather", x.Category));
    }

    [Fact]
    public async Task GetAggregatedDataAsync_FiltersByDate()
    {
        // Arrange
        var date = DateTime.Today;
        var items = new[]
        {
            CreateItem(date: date.AddDays(-1)),
            CreateItem(date: date)
        };

        var client = Substitute.For<IApiClient>();
        client.FetchAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateApiClientResult(items)));

        var service = new AggregationService(new[] { client });
        var request = new AggregationRequest { Date = date };

        // Act
        var response = await service.GetAggregatedDataAsync(request, CancellationToken.None);
        var result = response.Items.ToList();

        // Assert
        Assert.Single(result);
        Assert.All(result, x => Assert.True(x.Date >= date));
    }

    [Theory]
    [InlineData("date")]
    [InlineData("title")]
    [InlineData("source")]
    [InlineData("category")]
    public async Task GetAggregatedDataAsync_SortsByField_Ascending(string sortBy)
    {
        // Arrange
        var items = new[]
        {
            CreateItem(title: "B", source: "B", category: "B", date: DateTime.Today.AddDays(1)),
            CreateItem(title: "A", source: "A", category: "A", date: DateTime.Today)
        };

        var client = Substitute.For<IApiClient>();
        client.FetchAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateApiClientResult(items)));

        var service = new AggregationService(new[] { client });
        var request = new AggregationRequest { SortBy = sortBy, Descending = false };

        // Act
        var response = await service.GetAggregatedDataAsync(request, CancellationToken.None);
        var list = response.Items.ToList();

        // Assert
        switch (sortBy.ToLower())
        {
            case "date":
                Assert.True(list[0].Date <= list[1].Date);
                break;
            case "title":
                Assert.True(string.Compare(list[0].Title, list[1].Title, StringComparison.Ordinal) < 0);
                break;
            case "source":
                Assert.True(string.Compare(list[0].Source, list[1].Source, StringComparison.Ordinal) < 0);
                break;
            case "category":
                Assert.True(string.Compare(list[0].Category, list[1].Category, StringComparison.Ordinal) < 0);
                break;
        }
    }

    [Theory]
    [InlineData("date")]
    [InlineData("title")]
    [InlineData("source")]
    [InlineData("category")]
    public async Task GetAggregatedDataAsync_SortsByField_Descending(string sortBy)
    {
        // Arrange
        var items = new[]
        {
            CreateItem(title: "A", source: "A", category: "A", date: DateTime.Today),
            CreateItem(title: "B", source: "B", category: "B", date: DateTime.Today.AddDays(1))
        };

        var client = Substitute.For<IApiClient>();
        client.FetchAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateApiClientResult(items)));

        var service = new AggregationService(new[] { client });
        var request = new AggregationRequest { SortBy = sortBy, Descending = true };

        // Act
        var response = await service.GetAggregatedDataAsync(request, CancellationToken.None);
        var list = response.Items.ToList();

        // Assert
        switch (sortBy.ToLower())
        {
            case "date":
                Assert.True(list[0].Date >= list[1].Date);
                break;
            case "title":
                Assert.True(string.Compare(list[0].Title, list[1].Title, StringComparison.Ordinal) > 0);
                break;
            case "source":
                Assert.True(string.Compare(list[0].Source, list[1].Source, StringComparison.Ordinal) > 0);
                break;
            case "category":
                Assert.True(string.Compare(list[0].Category, list[1].Category, StringComparison.Ordinal) > 0);
                break;
        }
    }
}