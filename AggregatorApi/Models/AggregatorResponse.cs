namespace AggregatorApi.Models;

/// <summary>
/// Represents the response from the aggregation endpoint, including aggregated items and any error messages from failed API calls.
/// </summary>
public record AggregatorResponse(IEnumerable<AggregatedItem> Items, IEnumerable<string>? ErrorMessages = null)
{
    /// <summary>
    /// Indicates if any errors occurred during aggregation.
    /// </summary>
    public bool HasError => ErrorMessages != null && ErrorMessages.Any();
}