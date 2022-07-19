using Microsoft.AspNetCore.Http;

namespace MinimalEndpoints.Extensions.Http.ModelBinding;

public interface IEndpointModelBinder
{
    bool CanHandle(string? contentType);
    ValueTask<TModel?> BindAsync<TModel>(HttpRequest request, CancellationToken cancellationToken);
}
