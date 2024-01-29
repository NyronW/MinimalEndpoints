using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace MinimalEndpoints.Extensions.Http.ContentNegotiation;

public interface IResponseNegotiator
{
    bool CanHandle(MediaTypeHeaderValue accept);
    Task Handle(HttpContext httpContext, object? model, int? statusCode, string? contentType = null, CancellationToken cancellationToken = default);
}

public abstract class ContentNegotiatorBase
{
    protected string CheckContentType(string? contentType, string defaultMimeType = "application/json")
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return $"{defaultMimeType}; charset=utf-8";
        }

        if (contentType.EndsWith("+", StringComparison.OrdinalIgnoreCase))
        {
            return contentType + defaultMimeType;
        }

        if (!contentType.Contains(defaultMimeType, StringComparison.OrdinalIgnoreCase))
        {
            return $"{defaultMimeType}; charset=utf-8";
        }

        return contentType;
    }

}