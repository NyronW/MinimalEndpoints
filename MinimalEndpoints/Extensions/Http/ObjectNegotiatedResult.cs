using Microsoft.AspNetCore.Http;

namespace MinimalEndpoints.Extensions.Http;

public class ObjectNegotiatedResult : IResult
{
    private readonly int _statusCode;
    private readonly object? _value;
    private readonly string? _contentType;

    public ObjectNegotiatedResult(int statusCode, object? value, string? contentType)
    {
        _statusCode = statusCode;
        _value = value;
        _contentType = contentType;
    }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        await httpContext.Response.SendAsync(_value, _statusCode, _contentType);
    }
}

public sealed class OkNegotiatedResult : ObjectNegotiatedResult
{
    public OkNegotiatedResult(object? value) : base(StatusCodes.Status200OK, value, null)
    {

    }
}

public sealed class NotFoundNegotiatedResult : ObjectNegotiatedResult
{
    public NotFoundNegotiatedResult(object? value) : base(StatusCodes.Status404NotFound, value, null)
    {

    }
}

public sealed class BadRequestNegotiatedResult : ObjectNegotiatedResult
{
    public BadRequestNegotiatedResult(object? error, string? contentType = null)
        : base(StatusCodes.Status400BadRequest, error, contentType)
    {

    }
}

public sealed class InternalServerErrorNegotiatedResult : ObjectNegotiatedResult
{
    public InternalServerErrorNegotiatedResult(object? error, string? contentType = null)
        : base(StatusCodes.Status500InternalServerError, error, contentType)
    {

    }
}

