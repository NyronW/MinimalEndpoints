using Microsoft.AspNetCore.Mvc.Filters;

namespace MinimalEndpoints;
/// <summary>
/// Minimal Endpoint configuration
/// </summary>
public class EndpointConfiguration
{
    internal static bool UseEndpointAuthorizationMiddlewareResultHandler = false;

    /// <summary>
    /// Default route prefix to use for all endpoints. This can be overriden by setting the RoutePrefix property on the Endpoint Attribute 
    /// that's decorating the endpoint class 
    /// </summary>
    public string? DefaultRoutePrefix { get; set; }
    /// <summary>
    /// Sets the default Swagger UI document to which an endpoint will be associated. This can be overriden by setting the GroupName property on the 
    /// Endpoint Attribute that's decorating the endpoint class 
    /// </summary>
    public string? DefaultGroupName { get; set; }
    public FilterCollection Filters { get; set; } = new();

    public void UseAuthorizationResultHandler() => UseEndpointAuthorizationMiddlewareResultHandler = true;
}