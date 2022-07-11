using Microsoft.AspNetCore.Http;

namespace MinimalEndpoints;

public interface IEndpoint
{
    string Pattern { get; }
    HttpMethod Method { get; }
    Delegate Handler { get; }
}
