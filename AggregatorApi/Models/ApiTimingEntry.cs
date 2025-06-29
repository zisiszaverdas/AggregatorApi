namespace AggregatorApi.Models;

/// <summary>
/// Represents a single timing entry for an API request, including elapsed time and timestamp.
/// </summary>
/// <param name="ElapsedMs">The elapsed time in milliseconds.</param>
/// <param name="Timestamp">The timestamp when the request was made.</param>
public record ApiTimingEntry(long ElapsedMs, DateTime Timestamp);
