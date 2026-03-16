namespace MinimalEndpoints.WebApiDemo.Endpoints.Attributes;

/// <summary>
/// Example custom metadata attribute for cache configuration.
/// This demonstrates how custom attributes can be automatically registered as endpoint metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class CacheAttribute : Attribute, MinimalEndpoints.IEndpointMetadataAttribute
{
    public int DurationSeconds { get; }
    public string? VaryByHeader { get; set; }

    public CacheAttribute(int durationSeconds)
    {
        DurationSeconds = durationSeconds;
    }

    // GetMetadata() is not required - the default implementation returns the attribute itself
    // Only override if you need to return multiple metadata objects or transform the attribute
}

