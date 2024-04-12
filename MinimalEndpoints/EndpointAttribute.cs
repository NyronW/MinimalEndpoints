namespace MinimalEndpoints;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class EndpointAttribute : Attribute
{
    public string RouteName { get; set; } = string.Empty;
    public string? TagName { get; set; } = string.Empty;
    public string? OperationId { get; set; } = null;
    public string? GroupName { get; set; } = null;
    public bool ExcludeFromDescription { get; set; } = false;
    public string? RoutePrefixOverride { get; set; } = null;
    public string? Description { get; set; } = null;
    public string? RateLimitingPolicyName { get; set; } = null!;
    public bool DisableRateLimiting { get; set; } = false;
}