using Microsoft.AspNetCore.Http;
using System.Reflection;

namespace MinimalEndpoints;

/// <summary>
/// Global configuration options for the MinimalEndpoints library.
/// </summary>
public class MinimalEndpointsOptions
{
    /// <summary>
    /// Gets or sets the delegate used for scanning assemblies to find endpoint markers,
    /// </summary>
    public Action<MinimalEndpointAssemblyMarkerCollection>? EndpointMarkerAssembly { get; set; }

    /// <summary>
    /// If set, this delegate will be invoked when a parameter-binding error occurs.
    /// The library will pass in a <see cref="BindingFailureContext"/> containing relevant
    /// info (e.g., parameter name, raw value, etc.). The delegate should handle writing an error
    /// response (e.g., a 400 Bad Request) and return a <see cref="Task"/>.
    ///
    /// If null, the library can fall back to a default response (e.g., returning a 400 + ProblemDetails).
    /// </summary>
    public Func<BindingFailureContext, Task>? BindingFailurePolicy { get; set; }
}


/// <summary>
/// Collects assemblies where MinimalEndpoints might discover endpoint handlers,
/// marker attributes, or other relevant classes.
/// </summary>
public class MinimalEndpointAssemblyMarkerCollection
{
    private readonly List<Assembly> _assemblies = [];

    /// <summary>
    /// Adds the specified assembly to the collection.
    /// </summary>
    /// <param name="assembly">The assembly containing endpoints or other features.</param>
    public void AddAssembly(Assembly assembly)
    {
        _assemblies.Add(assembly);
    }

    /// <summary>
    /// Adds the collection of assemblies to the collection.
    /// </summary>
    /// <param name="assemblies">The assembly containing endpoints or other features.</param>
    public void AddAssemblies(IEnumerable<Assembly> assemblies)
    {
        _assemblies.AddRange(assemblies);
    }

    /// <summary>
    /// Gets all assemblies that have been added to this collection.
    /// </summary>
    public IReadOnlyList<Assembly> Assemblies => _assemblies;
}

/// <summary>
/// Provides context about a parameter-binding failure, including
/// the current HttpContext and details of the failed binding.
/// </summary>
public record BindingFailureContext(
    HttpContext HttpContext,
    string ParameterName,
    string? RawValue,
    string ErrorMessage
);


