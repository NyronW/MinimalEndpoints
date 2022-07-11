using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MinimalEndpoints;

public abstract class EndpointBase
{

    public virtual IResult Ok()
    {
        return Results.Ok();
    }

    public virtual IResult Ok<T>(T value)
    {
        return Results.Ok(value);
    }

    public virtual IResult Created<T>(string url, T value)
    {
        return Results.Created(url, value);
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
        return Results.BadRequest(error);
    }

    public virtual IResult BadRequest(ValidationProblemDetails problemDetails)
    {
        return Results.ValidationProblem(problemDetails.Errors,
            detail: problemDetails.Detail, instance: problemDetails.Instance,
            statusCode: StatusCodes.Status400BadRequest, title: problemDetails.Title,
            type: problemDetails.Type, extensions: problemDetails.Extensions);
    }

    protected IResult InternalServerError(string error)
    {
        return Results.Problem(error, statusCode: StatusCodes.Status500InternalServerError);
    }

    protected IResult InternalServerError<TProblem>(TProblem problem) where TProblem : ProblemDetails
    {
        return Results.Problem(problem);
    }

    public virtual IResult NotFound()
    {
        return Results.NotFound();
    }

    public virtual IResult NotFound<T>(T value)
    {
        return Results.NotFound(value);
    }

    public virtual IResult StatusCode(int statusCode)
    {
        return Results.StatusCode(statusCode);
    }

    public virtual IResult Unauthorized()
    {
        return Results.Unauthorized();
    }
}
