namespace MinimalEndpoints;

public abstract class Endpoint<TResponse> : IEndpoint
{
    public abstract string Pattern { get; }
    public abstract HttpMethod Method { get; }
    public abstract Task<TResponse> SendAsync();

    public Delegate Handler => HandlerCore;

    private async Task<TResponse> HandlerCore()
    {
        return await SendAsync();
    }
}

public abstract class Endpoint<TRequest, TResponse> : IEndpoint
{
    public abstract string Pattern { get; }
    public abstract HttpMethod Method { get; }
    public abstract Task<TResponse> SendAsync(TRequest request);

    public Delegate Handler => SendAsync;
}

public abstract class GetByIdEndpoint<TResponse> : Endpoint<int, TResponse>
{
    public override HttpMethod Method => HttpMethod.Get;
}