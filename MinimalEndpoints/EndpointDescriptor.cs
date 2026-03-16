using Microsoft.AspNetCore.Http;

namespace MinimalEndpoints;

public sealed class EndpointDescriptor
{
    public string Name { get; }
    public string ClassName { get; }
    public string Pattern { get; }
    public string HttpMethod { get; }
    public string HandlerMethod { get; }
    public string HandlerIdentifier { get; }
    public string RouteName { get; }
    public EndpointMetadataCollection? Metadata { get; }

    public EndpointDescriptor(string name, string className, string pattern, string httpMethod, string handlerMethod, string handlerIdentifier, string routeName = "",
        IReadOnlyList<object>? metadata = null)
    {
        Name = name;
        ClassName = className;
        Pattern = pattern;
        HttpMethod = httpMethod;
        HandlerMethod = handlerMethod;
        HandlerIdentifier = handlerIdentifier;
        RouteName = routeName;
        Metadata = metadata != null ? new(metadata) : new();
    }
}

public sealed class EndpointDescriptors
{
    private readonly List<EndpointDescriptor> _descriptors;

    public EndpointDescriptors()
    {
        _descriptors = [];
    }

    public IReadOnlyCollection<EndpointDescriptor> Descriptors => _descriptors.AsReadOnly();

    public IServiceProvider ServiceProvider { get; internal set; }

    internal void Add(EndpointDescriptor descriptor)
    {
        if (!string.IsNullOrWhiteSpace(descriptor.RouteName) &&
                _descriptors.Any(d => d.RouteName.Equals(descriptor.RouteName, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"An endpoint with the route name '{descriptor.RouteName}' already exists.");

        _descriptors.Add(descriptor);
    }
}