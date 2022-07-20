using Microsoft.AspNetCore.Http;

namespace MinimalEndpoints.Extensions.Http.ModelBinding;

public class EndpointModelBindingException : Exception, IHaveValidationProblemDetails
{
    public EndpointModelBindingException(string errorMessage, Exception? exception = null,
        IDictionary<string, string[]>? errors = null,
        string? instance = null) : base(errorMessage, exception)
    {
        Detail = errorMessage;
        Errors = errors ?? new Dictionary<string, string[]>();
        Instance = instance ?? string.Empty;
    }

    public string Type => "https://httpstatuses.com/400";

    public string Detail { get; }

    public string Title => "One or more validation error occured.";

    public string Instance { get; }

    public int Status => StatusCodes.Status400BadRequest;

    public IDictionary<string, string[]> Errors { get; }
}
