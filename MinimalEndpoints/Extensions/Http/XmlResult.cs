using Microsoft.AspNetCore.Http;
using Microsoft.IO;
using System.Xml.Serialization;

namespace MinimalEndpoints.Extensions.Http;


public class XmlResult<T> : IResult
{
    // The object to serialize
    private readonly T _result;
    private readonly int _statusCode;
    private readonly string? _contentType;

    public XmlResult(T result, int statusCode, string? contentType = "application/xml; charset=utf-8")
    {
        _result = result;
        _statusCode = statusCode;
        _contentType = contentType;
    }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        // Create a serializer for the model type
        var serializer = new XmlSerializer(_result == null ? typeof(object) : _result.GetType());

        // Rent a memory stream and serialize the model
        using var ms = StreamManager.Instance.GetStream();
        serializer.Serialize(ms, _result);
        ms.Position = 0;

        // Write the memory stream to the response Body
        httpContext.Response.StatusCode = _statusCode;
        httpContext.Response.ContentType = _contentType ?? "application/xml; charset=utf-8";
        await ms.CopyToAsync(httpContext.Response.Body);
    }
}

public static class StreamManager
{
    public static readonly RecyclableMemoryStreamManager Instance = new();
}
