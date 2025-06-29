using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace AggregatorApi.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate Next, ILogger<GlobalExceptionMiddleware> Logger)
{

    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await Next(context);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unhandled exception");

            var problemDetails = new ProblemDetails
            {
                Title = "An unexpected error occurred.",
                Status = (int)HttpStatusCode.InternalServerError,
                Detail = $"an undhandled error occured with traceId:{context.TraceIdentifier}",
                Instance = context.Request.Path
            };

            problemDetails.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = problemDetails.Status.Value;

            var json = JsonSerializer.Serialize(problemDetails, _jsonOptions);
            await context.Response.WriteAsync(json);
        }
    }
}