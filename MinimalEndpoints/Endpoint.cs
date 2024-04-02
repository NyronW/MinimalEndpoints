namespace MinimalEndpoints;

public abstract class Endpoint<TResponse> : EndpointBase, IEndpoint
{
    public abstract string Pattern { get; }
    public abstract HttpMethod Method { get; }

    [HandlerMethod]
    public abstract Task<TResponse> SendAsync(CancellationToken cancellationToken = default);

    public Delegate Handler => HandlerCore;

    protected virtual async Task<TResponse> HandlerCore(CancellationToken cancellationToken = default)
    {
        return await SendAsync(cancellationToken);
    }
}

public abstract class Endpoint<TRequest, TResponse> : EndpointBase, IEndpoint
{
    public abstract string Pattern { get; }
    public abstract HttpMethod Method { get; }

    [HandlerMethod]
    public abstract Task<TResponse> SendAsync(TRequest request, CancellationToken cancellationToken = default);

    public Delegate Handler => SendAsync;
}


public abstract class GetByIdEndpoint<TResponse, TKey> : Endpoint<TKey, TResponse>
{
    public override HttpMethod Method => HttpMethod.Get;
}

public abstract class GetByIdEndpoint<TResponse> : Endpoint<int, TResponse>
{
    public override HttpMethod Method => HttpMethod.Get;
}