namespace AggregatorApi.Services;

public interface ISystemClock
{
    DateTime UtcNow { get; }
}
