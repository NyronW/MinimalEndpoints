using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MinimalEndpoints;

public interface IEndpoint
{
    string Pattern { get; }
    HttpMethod Method { get; }
    Delegate Handler { get; }

    ValueTask<object[]> BindAsync(HttpRequest request, CancellationToken cancellationToken = default)
    {
        return default!;
    }

    RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        // Check if the current instance implements IEndpointDefinition
        if (this is IEndpointDefinition customDefinition)
        {
            return customDefinition.MapEndpoint(app);
        }

        // Default mapping logic
        return app.MapMethods(Pattern, [Method.Method], Handler);
    }
}

public interface IEndpointDefinition
{
    RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app);
}