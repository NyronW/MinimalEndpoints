using Microsoft.AspNetCore.Http;

namespace MinimalEndpoints;

public interface IEndpoint
{
    string Pattern { get; }
    HttpMethod Method { get; }
    Delegate Handler { get; }

    ValueTask<object> BindAsync(HttpRequest request, CancellationToken cancellationToken = default)
    {
        return default!; 
    }
}