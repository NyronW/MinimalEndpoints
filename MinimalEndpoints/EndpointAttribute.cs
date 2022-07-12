namespace MinimalEndpoints;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class EndpointAttribute : Attribute
{
    public string? TagName { get; set; } = string.Empty;
    public string? OperationId { get; set; } = null;
    public bool ExcludeFromDescription { get; set; } = false;
    public string? RoutePrefixOverride { get; set; } = null;
}