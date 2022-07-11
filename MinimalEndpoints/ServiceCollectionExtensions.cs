﻿using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MinimalEndpoints;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers endpoint from assemblies that contain specified types
    /// </summary>
    /// <param name="services">IServiceCollection instance</param>
    /// <param name="endpointAssemblyMarkerTypes">Marker type used to scan assembly</param>
    /// <returns>Service Collection</returns>
    public static IServiceCollection AddMinimalEndpoints(this IServiceCollection services, params Type[] endpointAssemblyMarkerTypes)
        => services.AddMinimalEndpoints(endpointAssemblyMarkerTypes.Select(t => t.GetTypeInfo().Assembly));

    /// <summary>
    /// Registers commands from the specified assemblies
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
    public static IServiceCollection AddMinimalEndpoints(this IServiceCollection services, IEnumerable<Assembly> assemblies)
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