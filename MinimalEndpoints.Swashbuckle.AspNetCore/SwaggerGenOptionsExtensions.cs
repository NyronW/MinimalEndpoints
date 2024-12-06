using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalEndpoints.Swashbuckle.AspNetCore;

public static class SwaggerGenOptionsExtensions
{
    /// <summary>
    /// Inject human-friendly descriptions for Operations, Parameters and Schemas based on XML Comment files
    /// </summary>
    /// <param name="swaggerGenOptions"></param>
    /// <param name="files">A collection of files that contains XML Comments</param>
    /// </param>
    public static void IncludeXmlComments(this SwaggerGenOptions swaggerGenOptions, IEnumerable<string> files,
        bool caseSensitiveRouteMatching = true)
    {
        swaggerGenOptions.DocumentFilter<EndpointXmlCommentsDocumentFilter>(files);
        if (!caseSensitiveRouteMatching)
            swaggerGenOptions.DocumentFilter<RouteTemplateCaseDocumentFilter>();
    }
}
