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

public sealed class OkNegotiatedResult(object? value) : ObjectNegotiatedResult(StatusCodes.Status200OK, value, null)
{
}

public sealed class NotFoundNegotiatedResult(object? value) : ObjectNegotiatedResult(StatusCodes.Status404NotFound, value, null)
{
}

public sealed class BadRequestNegotiatedResult(object? error, string? contentType = null) 
    : ObjectNegotiatedResult(StatusCodes.Status400BadRequest, error, contentType)
{
}

public sealed class InternalServerErrorNegotiatedResult(object? error, string? contentType = null) 
    : ObjectNegotiatedResult(StatusCodes.Status500InternalServerError, error, contentType)
{
}

public sealed class UnAuthorizedNegotiatedResult(object? error, string? contentType = null) 
    : ObjectNegotiatedResult(StatusCodes.Status401Unauthorized, error, contentType)
{
}

public sealed class ForbiddenNegotiatedResult(object? error, string? contentType = null)
    : ObjectNegotiatedResult(StatusCodes.Status403Forbidden, error, contentType)
{
}

