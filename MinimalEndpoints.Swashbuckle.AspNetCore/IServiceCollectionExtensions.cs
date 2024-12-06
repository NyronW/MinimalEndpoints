using Microsoft.Extensions.DependencyInjection;

namespace MinimalEndpoints.Swashbuckle.AspNetCore
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddMinimalOpenApi(this IServiceCollection services)
        {
            services.AddTransient<EndpointXmlCommentsDocumentFilter>();
            services.AddTransient<RouteTemplateCaseDocumentFilter>();

            return services;
        }
    }
}
