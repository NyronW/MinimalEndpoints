namespace MinimalEndpoints.WebApiDemo.Endpoints.Attributes;

/// <summary>
/// Example custom metadata attribute to mark endpoints as deprecated.
/// This demonstrates custom metadata that can be used by API documentation or middleware.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class DeprecatedAttribute : Attribute, MinimalEndpoints.IEndpointMetadataAttribute
{
    public string? Reason { get; set; }
    public string? SunsetDate { get; set; }
    public string? AlternativeEndpoint { get; set; }

    public DeprecatedAttribute(string? reason = null)
    {
        Reason = reason;
    }

    public IEnumerable<object> GetMetadata()
    {
        yield return this;
    }
}

