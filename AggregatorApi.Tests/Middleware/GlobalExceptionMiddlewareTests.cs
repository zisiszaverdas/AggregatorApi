using System.Net;
using System.Text.Json;
using AggregatorApi.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AggregatorApi.Tests.Middleware;

public class GlobalExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenNoException_CallsNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var logger = Substitute.For<ILogger<GlobalExceptionMiddleware>>();
        var wasCalled = false;
        RequestDelegate next = ctx =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        };
        var middleware = new GlobalExceptionMiddleware(next, logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(wasCalled);
        logger.DidNotReceiveWithAnyArgs().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task InvokeAsync_WhenExceptionThrown_ReturnsProblemDetailsJson()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-123";
        context.Request.Path = "/test-path";
        context.Response.Body = new MemoryStream();
        var logger = Substitute.For<ILogger<GlobalExceptionMiddleware>>();
        RequestDelegate next = ctx => throw new InvalidOperationException("fail!");
        var middleware = new GlobalExceptionMiddleware(next, logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var problem = JsonSerializer.Deserialize<ProblemDetails>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(problem);
        Assert.Equal("An unexpected error occurred.", problem.Title);
        Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);
        Assert.Equal("/test-path", problem.Instance);
        Assert.Contains("trace-123", problem.Detail);
        logger.ReceivedWithAnyArgs().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}