﻿using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System.Xml.Serialization;

namespace MinimalEndpoints.Extensions.Http.ContentNegotiation;

public class XmlResponseNegotiator : ContentNegotiatorBase, IResponseNegotiator
{
    private readonly XmlSerializerFactory _serializerFactory = new XmlSerializerFactory();

    public bool CanHandle(MediaTypeHeaderValue accept)
    {
        return accept.MediaType.ToString().Contains("xml", StringComparison.OrdinalIgnoreCase);
    }

    public async Task Handle(HttpContext httpContext, object? model, int? statusCode, string? contentType, CancellationToken cancellationToken)
    {
        // Create a serializer for the model type
        var type = model?.GetType() ?? typeof(object);
        var serializer = _serializerFactory.CreateSerializer(type);

        // Rent a memory stream and serialize the model
        using var ms = StreamManager.Instance.GetStream();
        serializer.Serialize(ms, model);
        ms.Position = 0;

        httpContext.Response.ContentType = CheckContentType(contentType, "xml") ?? "application/xml; charset=utf-8";

        if (statusCode.HasValue) httpContext.Response.StatusCode = statusCode.Value;

        // Write the memory stream to the response Body
        await ms.CopyToAsync(httpContext.Response.Body, cancellationToken);
    }
}
