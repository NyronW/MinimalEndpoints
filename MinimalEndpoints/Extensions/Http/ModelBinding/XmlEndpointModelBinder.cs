using Microsoft.AspNetCore.Http;

namespace MinimalEndpoints.Extensions.Http.ModelBinding;

public class XmlEndpointModelBinder : IEndpointModelBinder
{
    public bool CanHandle(string? contentType)
        => contentType?.IndexOf("xml", StringComparison.OrdinalIgnoreCase) != -1;

    public async ValueTask<TModel?> BindAsync<TModel>(HttpRequest request, CancellationToken cancellationToken)
    {
        TModel? model = default;

        if (request.HasXmlContentType())
            model = await request.ReadFromXmlAsync<TModel>(cancellationToken);

        return model;
    }
}