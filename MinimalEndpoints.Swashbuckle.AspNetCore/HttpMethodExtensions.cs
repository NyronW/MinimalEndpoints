using Microsoft.OpenApi.Models;

namespace MinimalEndpoints.Swashbuckle.AspNetCore;

public static class HttpMethodExtensions
{
    public static OperationType ToOpenApiOperationMethod(this string httpMethod)
    {
        return httpMethod.ToUpper() switch
        {
            "GET" => OperationType.Get,
            "POST" => OperationType.Post,
            "PUT" => OperationType.Put,
            "DELETE" => OperationType.Delete,
            "PATCH" => OperationType.Patch,
            "OPTIONS" => OperationType.Options,
            "HEAD" => OperationType.Head,
            _ => throw new ArgumentOutOfRangeException(nameof(httpMethod), $"Unsupported HTTP method: {httpMethod}.")
        };
    }
}
