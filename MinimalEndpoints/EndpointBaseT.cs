using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MinimalEndpoints.Extensions;
using MinimalEndpoints.Extensions.Http;
using MinimalEndpoints.Extensions.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalEndpoints;

public abstract class EndpointBase<TRequest, TResponse> : IEndpoint
{
    protected readonly ILogger _logger;

    protected EndpointBase(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(GetType().Name);
    }

    public abstract string Pattern { get; }
    public abstract HttpMethod Method { get; }
    public abstract Task<IResult> HandleRequestAsync(TRequest request, HttpRequest httpRequest, CancellationToken cancellationToken = default);

    public Delegate Handler => HandlerCore;

    protected virtual async Task<IResult> HandlerCore(HttpRequest httpRequest, CancellationToken cancellationToken = default)
    {
        var correlationId = httpRequest.Headers["X-CorrelationId"].FirstOrDefault() ?? Guid.NewGuid().ToString();
        httpRequest.HttpContext.Response.Headers.Add("X-CorrelationId", correlationId);

        using (_logger.AddContext("CorrelationId", correlationId))
        {
            TRequest? request = await httpRequest.GetModelAsync<TRequest>(cancellationToken);

            var validationErrors = await ValidateAsync(request);

            if (validationErrors.Count() > 0)
            {
                _logger.LogDebug("One or more validation errors occured");

                var errorDictionary = validationErrors
                    .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
                    .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());

                var problem = new ValidationProblemDetails(errorDictionary)
                {
                    Type = "https://httpstatuses.com/400",
                    Title = "One or more validation error occured.",
                    Detail = "Please refer to the errors property for additional details.",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = httpRequest.Path.Value
                };

                return Results.Extensions.Problem(problem);
            }

            try
            {
                return await HandleRequestAsync(request, httpRequest, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError("Unhandled exception occured", e);

                IHaveProblemDetails? exceptionDetails = e is IHaveProblemDetails ? (IHaveProblemDetails)e : null;

                var problem = new ProblemDetails
                {
                    Type = exceptionDetails?.Type ?? "https://httpstatuses.com/500",
                    Title = exceptionDetails?.Title ?? "An internal server error occured.",
                    Detail = exceptionDetails?.Detail ?? "Please retry your last request or contact of support team",
                    Status = exceptionDetails?.Status ?? StatusCodes.Status500InternalServerError,
                    Instance = exceptionDetails?.Instance ?? httpRequest.Path.Value,
                };

                return Results.Extensions.Problem(problem);
            }
        }
    }

    public virtual Task<IEnumerable<ValidationError>> ValidateAsync(TRequest request)
    {
        IEnumerable<ValidationError> errors = new List<ValidationError>();

        return Task.FromResult(errors);
    }

    protected IResult Ok(object? value)
    {
        return Results.Extensions.Ok(value);
    }

    protected IResult Created(string uri, object? value)
    {
        return Results.Extensions.Created(uri, value);
    }

    protected IResult CreatedAtRoute(string routeName, object? routeValues = null, object? value = null)
    {
        return Results.Extensions.CreatedAtRoute(routeName, routeValues, value);
    }

    protected IResult BadRequest(HttpValidationProblemDetails problem)
        => BadRequest(problem, "application/problem+");

    protected IResult BadRequest(object? error, string? contentType)
    {
        return Results.Extensions.BadRequest(error, contentType);
    }

    protected IResult Problem(ProblemDetails problem) => InternalServerError(problem, "application/problem+");

    protected IResult InternalServerError(object? error, string? contentType)
    {
        return Results.Extensions.InternalServerError(error, contentType);
    }

    protected IResult NotFound(object? value)
    {
        return Results.Extensions.NotFound(value);
    }
}