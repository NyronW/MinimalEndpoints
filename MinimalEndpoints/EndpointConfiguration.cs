using Microsoft.AspNetCore.Mvc.Filters;

namespace MinimalEndpoints;

public class EndpointConfiguration
{
    public string? DefaultRoutePrefix { get; set; }
    public FilterCollection Filters { get; set; } = new();
}