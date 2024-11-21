using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MinimalEndpoints.Authorization;
using MinimalEndpoints.Extensions.Http.ContentNegotiation;
using MinimalEndpoints.Extensions.Http.ModelBinding;
using System.Reflection;

namespace MinimalEndpoints;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddMinimalEndpoints(this IServiceCollection services)
        => services.AddMinimalEndpoints([], scanAssemblies: true);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddMinimalEndpoints(this IServiceCollection services, bool scanAssemblies)
        => services.AddMinimalEndpoints([], scanAssemblies: scanAssemblies);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddMinimalEndpointFromCallingAssembly(this IServiceCollection services)
        => services.AddMinimalEndpoints([], entryAssembly: Assembly.GetCallingAssembly(), scanAssemblies: false);
    /// <summary>
    /// Registers endpoint from assemblies that contain specified types
    /// </summary>
    /// <param name="services">IServiceCollection instance</param>
    /// <param name="endpointAssemblyMarkerTypes">Marker type used to scan assembly</param>
    /// <returns>Service Collection</returns>
    public static IServiceCollection AddMinimalEndpoints(this IServiceCollection services, params Type[] endpointAssemblyMarkerTypes)
        => services.AddMinimalEndpoints(endpointAssemblyMarkerTypes.Select(t => t.GetTypeInfo().Assembly));
    /// <summary>
    /// Registers endpoints from the specified assemblies
    /// </summary>
    /// <param name="services">IServiceCollection instance</param>
    /// <param name="assemblies">Assemblies to scan</param>
    /// <returns>Service Collection</returns>
    public static IServiceCollection AddMinimalEndpoints(this IServiceCollection services, params Assembly[] assemblies)
        => services.AddMinimalEndpoints(assemblies.Select(a => a));

    /// <summary>
    /// Registers commands from the specified assemblies
    /// </summary>
    /// <param name="services">IServiceCollection instance</param>
    /// <param name="assemblies">Assemblies to scan</param>
    /// <returns>Service Collection</returns>
    public static IServiceCollection AddMinimalEndpoints(this IServiceCollection services, IEnumerable<Assembly> assemblies, Assembly entryAssembly = null!, bool scanAssemblies = true)
    {
        if (assemblies == null || !assemblies.Any())
        {
            if (scanAssemblies)
                assemblies = AppDomain.CurrentDomain.GetAssemblies();
            else if (entryAssembly is { })
                assemblies = [entryAssembly];
        }

        assemblies = assemblies?.Distinct().ToArray() ?? [];

        var interfaceTypes = new[] { typeof(IEndpoint), typeof(IEndpointDefinition) };

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.ExportedTypes.Where(a => !a.IsAbstract && 
                (typeof(IEndpoint).IsAssignableFrom(a) || typeof(IEndpointDefinition).IsAssignableFrom(a))))
            {
                var registered = services.Any(sd => sd.ImplementationType == type);
                if (registered) continue;

                var interfaces = type.GetInterfaces();
                foreach (var @interface in interfaces.Where(i => interfaceTypes.Any(t => t == i)))
                {
                    //services.AddScoped(type);
                    services.AddScoped(@interface, type);
                }
            }
        }

        RegisterMinimalEndpointServices(services);

        return services;
    }

    private static void RegisterMinimalEndpointServices(IServiceCollection services)
    {
        // Check if EndpointDescriptors is already registered
        if (services.Any(sd => sd.ServiceType == typeof(EndpointDescriptors)))
        {
            return; // Registration already done
        }

        var descriptions = new EndpointDescriptors();

        services.AddSingleton(sp =>
        {
            descriptions.ServiceProvider = sp;
            return descriptions;
        });

        services.AddSingleton<EndpointHandler>();

        services.AddSingleton<IAuthorizationMiddlewareResultHandler, EndpointAuthorizationMiddlewareResultHandler>();

        services.AddTransient<IResponseNegotiator, JsonResponseNegotiator>();
        services.AddTransient<IResponseNegotiator, XmlResponseNegotiator>();

        services.AddTransient<IEndpointModelBinder, JsonEndpointModelBiner>();
        services.AddTransient<IEndpointModelBinder, XmlEndpointModelBinder>();
    }
}
