using System;

namespace AggregatorApi.Services
{
    /// <summary>
    /// Default implementation of ISystemClock using DateTime.UtcNow.
    /// </summary>
    public class SystemClock : ISystemClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}