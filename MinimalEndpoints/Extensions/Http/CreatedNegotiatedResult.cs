using Microsoft.AspNetCore.Http;

namespace MinimalEndpoints.Extensions.Http;

public sealed class CreatedNegotiatedResult : IResult
{
    private readonly string _uri;
    private readonly object? _value;

    public CreatedNegotiatedResult(string uri, object? value)
    {
        _uri = uri;
        _value = value;
    }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.Headers.Location = _uri;

        await httpContext.Response.SendAsync(_value, StatusCodes.Status201Created);
    }
}
