using Microsoft.AspNetCore.Http;

namespace MinimalEndpoints.Extensions.Http;

public sealed class CorrelationIdFilter(string headerName) : IEndpointFilter
{
    private readonly string _headerName = headerName ?? throw new ArgumentNullException(nameof(headerName));

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        if (!httpContext.Request.Headers.TryGetValue(_headerName, out var correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        httpContext.Response.OnStarting(() =>
        {
            httpContext.Response.Headers[_headerName] = correlationId;
            return Task.CompletedTask;
        });

        return await next(context);
    }
}

