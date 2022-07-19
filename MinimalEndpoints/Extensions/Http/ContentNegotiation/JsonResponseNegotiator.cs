using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MinimalEndpoints.Extensions.Http.ContentNegotiation;

public class JsonResponseNegotiator : ContentNegotiatorBase, IResponseNegotiator
{
    private readonly JsonSerializerOptions jsonSettings;

    public JsonResponseNegotiator()
    {
        jsonSettings = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    public bool CanHandle(MediaTypeHeaderValue accept)
    {
        return accept.MediaType.ToString().IndexOf("json", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public async Task Handle(HttpContext httpContext, object model, int? statusCode, string? contentType, CancellationToken cancellationToken)
    {
        httpContext.Response.ContentType = CheckContentType(contentType, "json") ?? "application/json; charset=utf-8";
        if (statusCode.HasValue) httpContext.Response.StatusCode = statusCode.Value;

        await JsonSerializer.SerializeAsync(httpContext.Response.Body, model, model == null ? typeof(object) : model.GetType(), jsonSettings, cancellationToken);
    }
}