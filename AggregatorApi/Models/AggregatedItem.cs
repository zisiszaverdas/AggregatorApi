namespace AggregatorApi.Models;

/// <summary>
/// Represents a single item aggregated from an external API, including its source, title, description, date, and category.
/// </summary>
/// <param name="Source">The name of the source API.</param>
/// <param name="Title">The title of the item.</param>
/// <param name="Description">The description of the item.</param>
/// <param name="Date">The date associated with the item.</param>
/// <param name="Category">The category of the item (e.g., Weather, News).</param>
public record AggregatedItem(
    string Source,
    string Title,
    string Description,
    DateTime Date,
    string Category
);