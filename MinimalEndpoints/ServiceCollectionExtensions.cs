using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MinimalEndpoints;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMinimalEndpoints(this IServiceCollection services, params Assembly[] assemblies)
    {
        if (assemblies == null || !assemblies.Any())
        {
            assemblies = AppDomain.CurrentDomain.GetAssemblies();
        }

        assemblies = assemblies.Distinct().ToArray();

        var interfaceTypes = new[] { typeof(IEndpoint) };

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetExportedTypes().Where(a => !a.IsAbstract))
            {
                if (!interfaceTypes.Any(t => t.IsAssignableFrom(type))) continue;

                var interfaces = type.GetInterfaces();
                foreach (var @interface in interfaces)
                {
                    if (!interfaceTypes.Any(t => t == @interface)) continue;

                    services.AddTransient(@interface, type);
                }
            }
        }

        return services;
    }
}
