namespace AggregatorApi.Models;

public record AggregatedItem(
    string Source,
    string Title,
    string Description,
    DateTime Date,
    string Category,
    bool Success
);