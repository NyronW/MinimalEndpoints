using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace MinimalEndpoints.Extensions.Http.ContentNegotiation;

public interface IResponseNegotiator
{
    bool CanHandle(MediaTypeHeaderValue accept);
    Task Handle(HttpContext httpContext, object model, int? statusCode, string? contentType = null, CancellationToken cancellationToken = default);
}

public abstract class ContentNegotiatorBase
{
    protected string? CheckContentType(string? contentType, string mimeType)
    {
        if (contentType is { } && contentType.IndexOf(mimeType, StringComparison.OrdinalIgnoreCase) == -1)
        {
            if (contentType.EndsWith("+", StringComparison.OrdinalIgnoreCase))
                contentType += mimeType;
            else
                contentType = "application/xml; charset=utf-8";
        }

        return contentType;
    }
}