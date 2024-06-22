using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MinimalEndpoints.Extensions.Http;

namespace MinimalEndpoints;

public abstract class EndpointBase
{
    internal readonly EndpointFilterCollection EndpointFilters = [];

    /// <summary>
    /// Adds an endpoint filter to current minimal endpoint
    /// </summary>
    /// <typeparam name="TFilter"></typeparam>
    protected void AddEndpointFilter<TFilter>() where TFilter: IEndpointFilter
    {
        EndpointFilters.Add(ActivatorUtilities.CreateInstance<TFilter>(EndpointRouteBuilderExtensions.ServiceProvider));
    }

    /// <summary>
    /// Adds an endpoint filter to current minimal endpoint
    /// </summary>
    /// <param name="filter"></param>
    protected void AddEndpointFilter(IEndpointFilter filter)
    {
        EndpointFilters.Add(filter);
    }

    #region Results
    public virtual IResult Ok()
    {
        return Results.Ok();
    }

    public virtual IResult Ok(object? value)
    {
        return Results.Extensions.Ok(value);
    }

    public virtual IResult Ok<T>(T value)
    {
        return Results.Extensions.Ok(value);
    }

    public virtual IResult Created(string uri, object? value)
    {
        return Results.Extensions.Created(uri, value);
    }

    public virtual IResult Created<T>(string url, T value)
    {
        return Results.Extensions.Created(url, value);
    }

    public virtual IResult CreatedAtRoute(string routeName, object? routeValues = null, object? value = null)
    {
        return Results.Extensions.CreatedAtRoute(routeName, routeValues, value);
    }

    public virtual IResult NoContent()
    {
        return Results.NoContent();
    }

    public virtual IResult Redirect(string url)
    {
        return Results.Redirect(url);
    }

    public virtual IResult RedirectPermanent(string url)
    {
        return Results.Redirect(url, permanent: true);
    }

    public virtual IResult RedirectPreserveMethod(string url)
    {
        return Results.Redirect(url, permanent: false, preserveMethod: true);
    }

    public virtual IResult RedirectPermanentPreserveMethod(string url)
    {
        return Results.Redirect(url, permanent: true, preserveMethod: true);
    }

    public virtual IResult LocalRedirect(string localUrl)
    {
        return Results.LocalRedirect(localUrl);
    }

    public virtual IResult LocalRedirectPermanent(string localUrl)
    {
        return Results.LocalRedirect(localUrl, permanent: true);
    }

    public virtual IResult LocalRedirectPreserveMethod(string localUrl)
    {
        return Results.LocalRedirect(localUrl, permanent: false, preserveMethod: true);
    }

    public virtual IResult BadRequest()
    {
        return Results.BadRequest();
    }

    public virtual IResult BadRequest<T>(T error)
    {
        return Results.Extensions.BadRequest(error);
    }

    public virtual IResult BadRequest(HttpValidationProblemDetails problem)
        => BadRequest(problem, "application/problem+");

    public virtual IResult BadRequest(object? error, string? contentType)
    {
        return Results.Extensions.BadRequest(error, contentType);
    }

    public virtual IResult BadRequest(ValidationProblemDetails problemDetails)
    {
        return Results.ValidationProblem(problemDetails.Errors,
            detail: problemDetails.Detail, instance: problemDetails.Instance,
            statusCode: StatusCodes.Status400BadRequest, title: problemDetails.Title,
            type: problemDetails.Type, extensions: problemDetails.Extensions);
    }

    public virtual IResult Problem(ProblemDetails problem) => InternalServerError(problem, "application/problem+");

    public virtual IResult InternalServerError(object? error, string? contentType)
    {
        return Results.Extensions.InternalServerError(error, contentType);
    }

    public virtual IResult InternalServerError(string error)
    {
        var pd = new ProblemDetails
        {
            Title = "An internal server error occurred.",
            Detail = error,
            Status = StatusCodes.Status500InternalServerError
        };
        return Results.Extensions.Problem(pd);
    }

    public virtual IResult InternalServerError<TProblem>(TProblem problem) where TProblem : ProblemDetails
    {
        return Results.Extensions.Problem(problem);
    }

    public virtual IResult NotFound()
    {
        return Results.NotFound();
    }

    public virtual IResult NotFound(object? value)
    {
        return Results.Extensions.NotFound(value);
    }

    public virtual IResult NotFound<T>(T value)
    {
        return Results.Extensions.NotFound(value);
    }

    public virtual IResult StatusCode(int statusCode)
    {
        return Results.StatusCode(statusCode);
    }

    public virtual IResult Unauthorized()
    {
        return Results.Unauthorized();
    }

    public virtual IResult Forbidden()
    {
        return Results.Forbid();
    }
    #endregion
}
