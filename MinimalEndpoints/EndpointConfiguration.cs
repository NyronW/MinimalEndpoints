using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace MinimalEndpoints;
/// <summary>
/// Minimal Endpoint configuration
/// </summary>
public sealed class EndpointConfiguration
{
    internal static bool UseEndpointAuthorizationMiddlewareResultHandler = false;

    internal EndpointFilterCollection EndpointFilters { get; set; } = [];
    internal IServiceProvider ServiceProvider { get; set; } = default!;

    /// <summary>
    /// Default route prefix to use for all endpoints. This can be overriden by setting the RoutePrefix property on the Endpoint Attribute 
    /// that's decorating the endpoint class 
    /// </summary>
    public string? DefaultRoutePrefix { get; set; }
    /// <summary>
    /// Sets the default Swagger UI document to which an endpoint will be associated. This can be overriden by setting the GroupName property on the 
    /// EndpointAttribute that's decorating the endpoint class 
    /// </summary>
    public string? DefaultGroupName { get; set; }

    /// <summary>
    /// Sets the default rate limiting policy to apply to all endpoints. This can be overriden by setting the RateLimitingPolicyName or DisableRateLimiting
    /// property on the EndpointAttribute that's decorating the endpoint class
    /// </summary>
    public string? DefaultRateLimitingPolicyName { get; set; }


    public FilterCollection Filters { get; set; } = [];

    public void AddFilterMetadata<TMetadata>() where TMetadata : IFilterMetadata
    {
        Filters.Add(ActivatorUtilities.CreateInstance<TMetadata>(ServiceProvider));
    }

    public void AddFilterMetadata(IFilterMetadata filter)
    {
        Filters.Add(filter);
    }


    /// <summary>
    /// Adds an endpoint filter to all minimal endpoints.
    /// </summary>
    /// <typeparam name="TFilter"></typeparam>
    public void AddEndpointFilter<TFilter>() where TFilter : IEndpointFilter
    {
        EndpointFilters.Add(ActivatorUtilities.CreateInstance<TFilter>(ServiceProvider));    
    }

    /// <summary>
    /// Adds an endpoint filter to all minimal endpoints.
    /// </summary>
    /// <param name="filter"></param>
    public void AddEndpointFilter(IEndpointFilter filter)
    {
        EndpointFilters.Add(filter);
    }

    public void UseAuthorizationResultHandler() => UseEndpointAuthorizationMiddlewareResultHandler = true;
}

internal class EndpointFilterCollection : Collection<IEndpointFilter>
{

}
