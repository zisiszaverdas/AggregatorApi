namespace AggregatorApi.Services;

/// <summary>
/// Provides the current UTC time, abstracted for testability.
/// </summary>
public interface ISystemClock
{
    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    DateTime UtcNow { get; }
}
