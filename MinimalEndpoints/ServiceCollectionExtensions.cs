﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MinimalEndpoints.Authorization;
using MinimalEndpoints.Extensions.Http.ContentNegotiation;
using MinimalEndpoints.Extensions.Http.ModelBinding;
using MinimalEndpoints.Extensions;
using System.Reflection;
using System.Collections.Concurrent;

namespace MinimalEndpoints;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddMinimalEndpointFromCallingAssembly(this IServiceCollection services)
        => services.AddMinimalEndpoints(opts =>
        {
            opts.EndpointMarkerAssembly = marker =>
            {
                marker.AddAssembly(Assembly.GetCallingAssembly());
            };
        });
    /// <summary>
    /// Registers endpoint from assemblies that contain specified types
    /// </summary>
    /// <param name="services">IServiceCollection instance</param>
    /// <param name="endpointAssemblyMarkerTypes">Marker type used to scan assembly</param>
    /// <returns>Service Collection</returns>
    public static IServiceCollection AddMinimalEndpoints(this IServiceCollection services, params Type[] endpointAssemblyMarkerTypes)
        => services.AddMinimalEndpoints(opts =>
        {
            opts.EndpointMarkerAssembly = marker =>
            {
                marker.AddAssemblies(endpointAssemblyMarkerTypes.Select(t => t.GetTypeInfo().Assembly));
            };
        });

    /// <summary>
    /// Registers endpoints from the specified assemblies
    /// </summary>
    /// <param name="services">IServiceCollection instance</param>
    /// <param name="assemblies">Assemblies to scan</param>
    /// <returns>Service Collection</returns>
    public static IServiceCollection AddMinimalEndpoints(this IServiceCollection services, params Assembly[] assemblies)
        => services.AddMinimalEndpoints(opts =>
        {
            opts.EndpointMarkerAssembly = marker =>
            {
               marker.AddAssemblies(assemblies);
            }; 
        });

    /// <summary>
    /// Registers commands from the specified assemblies
    /// </summary>
    /// <param name="services">IServiceCollection instance</param>
    /// <param name="assemblies">Assemblies to scan</param>
    /// <returns>Service Collection</returns>
    public static IServiceCollection AddMinimalEndpoints(this IServiceCollection services, Action<MinimalEndpointsOptions>? configure = null)
    {
        var options = new MinimalEndpointsOptions();
        configure?.Invoke(options);

        if (options.BindingFailurePolicy is not null)
        {
            services.AddSingleton(options.BindingFailurePolicy);
        }

        var markerCollection = new MinimalEndpointAssemblyMarkerCollection();

        if (options.EndpointMarkerAssembly != null)
        {
            options.EndpointMarkerAssembly(markerCollection);
        }
        else
        {
            markerCollection.AddAssemblies(AppDomain.CurrentDomain.GetAssemblies());
        }

        var interfaceTypes = new[] { typeof(IEndpoint), typeof(IEndpointDefinition) };

        for (int i = 0; i < markerCollection.Assemblies.Count; i++)
        {
            Assembly assembly = markerCollection.Assemblies[i];
            var definedTypes = assembly.DefinedTypes.ToList();

            for (int n = 0; n < definedTypes.Count; n++)
            {
                TypeInfo? type = definedTypes[n];
                if (type.IsAbstract || !type.IsClass ||
                        !type.DerivedFromAny([typeof(IEndpoint), typeof(IEndpointDefinition)]))
                    continue;

                if (services.Any(sd => sd.ImplementationType == type)) continue;

                var interfaces = type.GetInterfaces();
                foreach (var @interface in interfaces)
                {
                    if (!interfaceTypes.Contains(@interface))
                        continue;

                    services.AddScoped(type);
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

        ConcurrentDictionary<Type, object?> _valueTypeInstances = new()
        {
            [typeof(int)] = 0,
            [typeof(bool)] = false,
            [typeof(double)] = 0.0,
            [typeof(float)] = 0.0f,
            [typeof(byte)] = (byte)0,
            [typeof(sbyte)] = (sbyte)0,
            [typeof(short)] = (short)0,
            [typeof(ushort)] = (ushort)0,
            [typeof(long)] = 0L,
            [typeof(ulong)] = 0UL,
            [typeof(uint)] = 0U,
            [typeof(char)] = '\0',
            [typeof(decimal)] = 0m,

            // Nullable types
            [typeof(int?)] = null,
            [typeof(bool?)] = null,
            [typeof(double?)] = null,
            [typeof(float?)] = null,
            [typeof(byte?)] = null,
            [typeof(sbyte?)] = null,
            [typeof(short?)] = null,
            [typeof(ushort?)] = null,
            [typeof(long?)] = null,
            [typeof(ulong?)] = null,
            [typeof(uint?)] = null,
            [typeof(char?)] = null,
            [typeof(decimal?)] = null,

            // Common structs
            [typeof(Guid)] = Guid.Empty,
            [typeof(Guid?)] = null,
            [typeof(DateTime)] = default(DateTime),
            [typeof(DateTime?)] = null,
            [typeof(TimeSpan)] = default(TimeSpan),
            [typeof(TimeSpan?)] = null,
            [typeof(DateOnly)] = default(DateOnly),
            [typeof(DateOnly?)] = null,
            [typeof(TimeOnly)] = default(TimeOnly),
            [typeof(TimeOnly?)] = null,
        };
        services.AddSingleton(sp => _valueTypeInstances);

        ConcurrentDictionary<MethodInfo, MethodDetails> _methodCache = new();
        services.AddSingleton(sp => _methodCache);

        ConcurrentDictionary<Type, (bool IsOverridden, MethodInfo? Method)> _bindingCache = new();
        services.AddSingleton(sp => _bindingCache);

        services.AddSingleton<EndpointHandler>();

        services.AddSingleton<IAuthorizationMiddlewareResultHandler, EndpointAuthorizationMiddlewareResultHandler>();

        services.AddTransient<IResponseNegotiator, JsonResponseNegotiator>();
        services.AddTransient<IResponseNegotiator, XmlResponseNegotiator>();

        services.AddTransient<IEndpointModelBinder, JsonEndpointModelBiner>();
        services.AddTransient<IEndpointModelBinder, XmlEndpointModelBinder>();
    }
}
