using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using MinimalEndpoints.Extensions;
using MinimalEndpoints.Extensions.Http;
using MinimalEndpoints.Extensions.Http.ModelBinding;
using MinimalEndpoints.Extensions.Validation;

namespace MinimalEndpoints;

public abstract class EndpointBase<TRequest, TResponse> : EndpointBase, IEndpoint
{
    protected readonly ILogger _logger;

    private HttpRequest _httpRequest = null!;

    protected EndpointBase(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(GetType().Name);
    }

    public abstract string Pattern { get; }
    public abstract HttpMethod Method { get; }


    [HandlerMethod]
    public abstract Task<IResult> HandleRequestAsync(TRequest request, HttpRequest httpRequest, CancellationToken cancellationToken = default);

    public Delegate Handler => HandlerCore;

    protected virtual async Task<IResult> HandlerCore(HttpRequest httpRequest, CancellationToken cancellationToken = default)
    {
        _httpRequest = httpRequest;

        var correlationIdHeader = GetHeaderValue<string>("X-CorrelationId");
        var correlationId = StringValues.IsNullOrEmpty(correlationIdHeader) ? Guid.NewGuid().ToString() : correlationIdHeader;
        httpRequest.HttpContext.Response.Headers["X-CorrelationId"] = correlationId;

        using (_logger.AddContext("CorrelationId", correlationId))
        using (_logger.AddContext("RequestUri", httpRequest.Path.Value))
        {
            try
            {
                TRequest? request = await httpRequest.GetModelAsync<TRequest>(cancellationToken);

                var validationErrors = await ValidateAsync(request!);

                if (validationErrors.Any())
                {
                    _logger.LogDebug("One or more validation errors occured");

                    var errorDictionary = new Dictionary<string, string[]>();
                    foreach (var error in validationErrors)
                    {
                        if (!errorDictionary.TryGetValue(error.PropertyName, out var errors))
                        {
                            errors = [];
                            errorDictionary[error.PropertyName] = errors;
                        }
                        errorDictionary[error.PropertyName] = [.. errors, error.ErrorMessage];
                    }

                    var problem = new ValidationProblemDetails(errorDictionary)
                    {
                        Type = "https://httpstatuses.com/400",
                        Title = "One or more validation error occured.",
                        Detail = "Please refer to the errors property for additional details.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = httpRequest.Path
                    };

                    return Results.Extensions.Problem(problem);
                }

                return await HandleRequestAsync(request!, httpRequest, cancellationToken);
            }
            catch (EndpointModelBindingException ex)
            {
                _logger.LogError(ex, "Unhandled exception occured while attempting to bind data");

                var exceptionDetails = (IHaveValidationProblemDetails)ex;

                var problem = new ValidationProblemDetails(exceptionDetails.Errors)
                {
                    Type = exceptionDetails.Type,
                    Title = exceptionDetails.Title,
                    Detail = exceptionDetails.Detail ?? "Please refer to the errors property for additional details.",
                    Status = exceptionDetails.Status,
                    Instance = exceptionDetails.Instance ?? httpRequest.Path,
                };

                return Results.Extensions.Problem(problem);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unhandled exception occurred while executing request for: {Path}", httpRequest.Path);

                var env = httpRequest.HttpContext.RequestServices.GetService<IWebHostEnvironment>();

                //throw error if in development environment
                if (env!.IsDevelopment()) throw;

                IHaveProblemDetails? exceptionDetails = e is IHaveProblemDetails details ? details : null;

                var problem = new ProblemDetails
                {
                    Type = exceptionDetails?.Type ?? "https://httpstatuses.com/500",
                    Title = exceptionDetails?.Title ?? "An internal server error occured.",
                    Detail = exceptionDetails?.Detail ?? "Please retry your last request or contact of support team",
                    Status = exceptionDetails?.Status ?? StatusCodes.Status500InternalServerError,
                    Instance = exceptionDetails?.Instance ?? httpRequest.Path,
                };

                return Results.Extensions.Problem(problem);
            }
        }
    }

    public virtual Task<IEnumerable<ValidationError>> ValidateAsync(TRequest request)
    {
        var errors = Array.Empty<ValidationError>();

        return Task.FromResult(errors.AsEnumerable());
    }

    protected TValue? GetRouteValue<TValue>(string name)
    {
        if (_httpRequest.RouteValues.TryGetValue(name, out var value) && value is TValue typedValue)
        {
            return typedValue;
        }
        return default;
    }

    protected TValue? GetQueryStringValue<TValue>(string name)
    {
        if (_httpRequest.Query.TryGetValue(name, out var values) && values.Count > 0)
        {
            var value = values[0];
            return (TValue?)Convert.ChangeType(value, typeof(TValue));
        }
        return default;
    }

    protected TValue? GetHeaderValue<TValue>(string name)
    {
        if (_httpRequest.Headers.TryGetValue(name, out var values) && values.Count > 0)
        {
            var value = values[0];
            return (TValue?)Convert.ChangeType(value, typeof(TValue));
        }
        return default;
    }

    protected TValue? GetFormValue<TValue>(string name)
    {
        if (_httpRequest.HasFormContentType && _httpRequest.Form.TryGetValue(name, out var values) && values.Count > 0)
        {
            var value = values[0];
            return (TValue?)Convert.ChangeType(value, typeof(TValue));
        }
        return default;
    }
}