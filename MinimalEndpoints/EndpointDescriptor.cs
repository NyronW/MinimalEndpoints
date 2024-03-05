using Microsoft.Extensions.FileSystemGlobbing.Internal;

namespace MinimalEndpoints;

public class EndpointDescriptor
{
    public string ClassName { get; }
    public string Pattern { get; }
    public string HttpMethod { get; }
    public string HandlerMethod { get; }


    public EndpointDescriptor(string className, string pattern, string httpMethod, string handlerMethod)
    {
        ClassName = className;
        Pattern = pattern;
        HttpMethod = httpMethod;
        HandlerMethod = handlerMethod;
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

    internal void Add(EndpointDescriptor descriptor)
    {
        _descriptors.Add(descriptor);
    }
}