namespace MinimalEndpoints.WebApiDemo.Endpoints.Attributes;

/// <summary>
/// Example custom metadata attribute for feature flagging.
/// This demonstrates how custom attributes can provide metadata for feature toggles.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class FeatureFlagAttribute : Attribute, MinimalEndpoints.IEndpointMetadataAttribute
{
    public string FlagName { get; }
    public bool Required { get; set; } = true;

    public FeatureFlagAttribute(string flagName)
    {
        FlagName = flagName;
    }

    public IEnumerable<object> GetMetadata()
    {
        yield return this;
    }
}

