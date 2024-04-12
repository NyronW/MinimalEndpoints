using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace MinimalEndpoints.Extensions.Http;

public class StreamResult<T> : IResult
{
    private readonly IAsyncEnumerable<T> _dataStream;
    private readonly JsonSerializerOptions _options;

    public StreamResult(IAsyncEnumerable<T> dataStream, JsonSerializerOptions options = null!)
    {
        _dataStream = dataStream;
        _options = options ?? new JsonSerializerOptions();
    }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.ContentType = "application/stream+json";

        await using var streamWriter = new StreamWriter(new BufferedStream(httpContext.Response.Body), leaveOpen: true);

        await foreach (var item in _dataStream)
        {
            await JsonSerializer.SerializeAsync(streamWriter.BaseStream, item, _options);
            await streamWriter.WriteLineAsync();
            await streamWriter.FlushAsync();
        }
    }
}
