using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalEndpoints.Extensions.Http;

public sealed class CreatedAtRouteNegotiatedResult : IResult
{
    private readonly string _routeName;
    private readonly object? _routeValues;
    private readonly object? _value;

    public CreatedAtRouteNegotiatedResult(string routeName, object? routeValues, object? value)
    {
        _routeName = routeName;
        _routeValues = routeValues;
        _value = value;
    }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var links = httpContext.RequestServices.GetRequiredService<LinkGenerator>();

        var uri = links.GetUriByName(httpContext, _routeName, _routeValues);
        httpContext.Response.Headers.Location = uri;

        await httpContext.Response.SendAsync(_value, StatusCodes.Status201Created);
    }
}