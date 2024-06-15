using Microsoft.AspNetCore.Http;

namespace MinimalEndpoints.Extensions.Http;

public static class ResultsExtensions
{
    public static IResult Xml<T>(this IResultExtensions _, T result, string? contentType, int? statusCode)
            => new XmlResult<T>(result, statusCode ?? 200, contentType);

    public static IResult Ok(this IResultExtensions _, object? value)
    => new OkNegotiatedResult(value);

    public static IResult CreatedAtRoute(this IResultExtensions _, string routeName,
        object? routeValues, object? value)
        => new CreatedAtRouteNegotiatedResult(routeName, routeValues, value);

    public static IResult Created(this IResultExtensions _, string uri, object? value)
        => new CreatedNegotiatedResult(uri, value);

    public static IResult BadRequest(this IResultExtensions _, object? error, string? contentType = null)
        => new BadRequestNegotiatedResult(error, contentType);

    public static IResult NotFound(this IResultExtensions _, object? value)
        => new NotFoundNegotiatedResult(value);

    public static IResult Problem(this IResultExtensions extensions, object? problem)
        => extensions.BadRequest(problem, "application/problem+");

    public static IResult InternalServerError(this IResultExtensions _, object? value, string? contentType = null)
            => new InternalServerErrorNegotiatedResult(value);

    public static IResult UnAuthorized(this IResultExtensions _, object? value, string? contentType = null)
        => new UnAuthorizedNegotiatedResult(value);

    public static IResult Forbidden(this IResultExtensions _, object? value, string? contentType = null)
    => new ForbiddenNegotiatedResult(value);
}