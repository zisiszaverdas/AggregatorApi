namespace AggregatorApi.Models;

public record ApiClientResult(IEnumerable<AggregatedItem> Items, string? ErrorMessage = null)
{
    public static ApiClientResult CreateErrorResult(string errorMessage, ILogger logger, Exception? ex = null)
    {
        logger.LogError(ex, errorMessage);
        return new ApiClientResult([], errorMessage);
    }
};