using Microsoft.AspNetCore.Http;

namespace MinimalEndpoints.Extensions.Http;

public sealed class CorrelationIdFilter(string headerName) : IEndpointFilter
{
    private readonly string _headerName = headerName ?? throw new ArgumentNullException(nameof(headerName));

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        if (!httpContext.Request.Headers.TryGetValue(_headerName, out var correlationId) || string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        if (!httpContext.Response.Headers.ContainsKey(_headerName))
        {
            httpContext.Response.Headers.Append(_headerName, correlationId);
        }

        return await next(context);
    }
}

