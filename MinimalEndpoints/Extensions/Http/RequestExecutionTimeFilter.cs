﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MinimalEndpoints.Extensions.Http;

public sealed class RequestExecutionTimeFilter(ILogger<RequestExecutionTimeFilter> logger) : IEndpointFilter
{
    private readonly ILogger<RequestExecutionTimeFilter> _logger = logger;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        var httpContext = context.HttpContext;
        var method = httpContext.Request.Method;
        var path = httpContext.Request.Path;

        try
        {
            var result = await next(context);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing the request {Method} {Path}", method, path);
            throw; 
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation("Request {Method} {Path} executed in {Duration}ms", method, path, stopwatch.ElapsedMilliseconds);
        }
    }
}