namespace MinimalEndpoints;

public interface IEndpointRegistrationContext
{
    void RegisterEndpoint(Type endpointType);
    void RegisterEndpointDefinition(Type definitionType);
}

