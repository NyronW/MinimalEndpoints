using Microsoft.Extensions.DependencyInjection;

namespace MinimalEndpoints.Swashbuckle.AspNetCore;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddMinimalEndpointSwaggerGen(this IServiceCollection services)
    {
        services.AddTransient<EndpointXmlCommentsDocumentFilter>();
        services.AddTransient<RouteTemplateCaseDocumentFilter>();

        return services;
    }

    public static bool DerivedFromAny(this Type type, IReadOnlyList<Type> types)
    {
        for (int i = 0; i < types.Count; i++)
        {
            if (types[i].IsAssignableFrom(type))
            {
                return true;
            }
        }
        return false;
    }
}
