using Polly;

namespace AggregatorApi.Extension;

public static class HttpClientBuilderExtensions
{
    public static IHttpClientBuilder AddApiClientResilience(this IHttpClientBuilder builder)
    {
        builder.AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(1);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.MinimumThroughput = 4;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(10);
        });
        return builder;
    }
}