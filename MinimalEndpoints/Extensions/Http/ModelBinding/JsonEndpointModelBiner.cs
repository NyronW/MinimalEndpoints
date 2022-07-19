using Microsoft.AspNetCore.Http;

namespace MinimalEndpoints.Extensions.Http.ModelBinding;

public class JsonEndpointModelBiner : IEndpointModelBinder
{
    public bool CanHandle(string? contentType)
        => contentType?.IndexOf("json", StringComparison.OrdinalIgnoreCase) != -1;


    public async ValueTask<TModel?> BindAsync<TModel>(HttpRequest request, CancellationToken cancellationToken)
    {
        TModel? model = default;

        if (request.HasJsonContentType())
            model = await request.ReadFromJsonAsync<TModel>(cancellationToken);
        else
            throw new InvalidOperationException($"Unable to read the request as JSON because the request content type '{request.ContentType}' is not a known JSON content type.");

        return model;
    }
}