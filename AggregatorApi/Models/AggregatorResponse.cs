namespace AggregatorApi.Models;

public record AggregatorResponse(IEnumerable<AggregatedItem> Items, IEnumerable<string>? ErrorMessages = null) {
    public bool HasError => ErrorMessages != null && ErrorMessages.Any();
};