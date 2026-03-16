namespace MinimalEndpoints;

/// <summary>
/// Marker interface for attributes that should be automatically registered as endpoint metadata.
/// Implement this interface on custom attributes to have them automatically added to endpoint metadata during registration.
/// </summary>
/// <remarks>
/// The <see cref="GetMetadata"/> method provides flexibility for attributes to:
/// <list type="bullet">
/// <item>Return multiple metadata objects (not just the attribute itself)</item>
/// <item>Transform attribute data into different metadata types</item>
/// <item>Create metadata objects that require additional processing</item>
/// </list>
/// For simple cases where you just want to register the attribute itself, you don't need to implement this method
/// as it has a default implementation that returns the attribute.
/// </remarks>
public interface IEndpointMetadataAttribute
{
    /// <summary>
    /// Gets the metadata objects to register with the endpoint.
    /// </summary>
    /// <returns>Collection of metadata objects to register. The default implementation returns the attribute itself.</returns>
    /// <remarks>
    /// Override this method when you need to:
    /// <list type="bullet">
    /// <item>Return multiple metadata objects from a single attribute</item>
    /// <item>Transform the attribute into a different metadata type</item>
    /// <item>Create metadata objects that require additional initialization</item>
    /// </list>
    /// For most use cases, the default implementation is sufficient.
    /// </remarks>
    IEnumerable<object> GetMetadata()
    {
        yield return this;
    }
}

