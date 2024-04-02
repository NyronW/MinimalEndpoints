namespace MinimalEndpoints;

public class EndpointDescriptor
{
    public string Name { get; }
    public string ClassName { get; }
    public string Pattern { get; }
    public string HttpMethod { get; }
    public string HandlerMethod { get; }
    public string HandlerIdentifier { get; }

    public EndpointDescriptor(string name, string className, string pattern, string httpMethod, string handlerMethod, string handlerIdentifier)
    {
        Name = name;
        ClassName = className;
        Pattern = pattern;
        HttpMethod = httpMethod;
        HandlerMethod = handlerMethod;
        HandlerIdentifier = handlerIdentifier;
    }
}

public class EndpointDescriptors
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
        _descriptors.Add(descriptor);
    }
}