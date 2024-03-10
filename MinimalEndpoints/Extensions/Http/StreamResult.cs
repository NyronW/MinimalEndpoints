using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace MinimalEndpoints.Extensions.Http;

public class StreamResult<T> : IResult
{
    private readonly IAsyncEnumerable<T> _dataStream;

    public StreamResult(IAsyncEnumerable<T> dataStream)
    {
        _dataStream = dataStream;
    }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var response = httpContext.Response;
        response.ContentType = "application/stream+json";

        await using var streamWriter = new StreamWriter(response.Body);
        await foreach (var item in _dataStream)
        {
            var json = JsonSerializer.Serialize(item);
            await streamWriter.WriteLineAsync(json);
            await streamWriter.FlushAsync();  // Ensure each item is flushed to the response stream
        }
    }
}
