using System.ComponentModel;

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

    public EndpointDescriptor(string name, string className, string pattern, string httpMethod, string handlerMethod, string handlerIdentifier, string routeName = "")
    {
        Name = name;
        ClassName = className;
        Pattern = pattern;
        HttpMethod = httpMethod;
        HandlerMethod = handlerMethod;
        HandlerIdentifier = handlerIdentifier;
        RouteName = routeName;
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

    public void Add(EndpointDescriptor descriptor)
    {
        if (!string.IsNullOrWhiteSpace(descriptor.RouteName) &&
                _descriptors.Any(d => d.RouteName.Equals(descriptor.RouteName, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"An endpoint with the route name '{descriptor.RouteName}' already exists.");

        _descriptors.Add(descriptor);
    }
}