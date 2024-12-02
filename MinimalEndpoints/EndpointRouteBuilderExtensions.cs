﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using System.Reflection;
using System.Text;

namespace MinimalEndpoints;

public static class EndpointRouteBuilderExtensions
{
    private static readonly ObjectPool<StringBuilder> StringBuilderPool =
        new DefaultObjectPoolProvider().CreateStringBuilderPool();

    internal static IServiceProvider ServiceProvider { get; set; } = null!;

    /// <summary>
    /// Configure Minimal endpints
    /// </summary>
    /// <param name="builder">EndpointRouteBuilder</param>
    /// <returns>EndpointRouteBuilder</returns>
    public static IEndpointRouteBuilder UseMinimalEndpoints(this IEndpointRouteBuilder builder)
        => builder.UseMinimalEndpoints(configuration: null);

    /// <summary>
    /// Configure Minimal endpints
    /// </summary>
    /// <param name="builder">EndpointRouteBuilder</param>
    /// <param name="configuration">Endpoint configuration object</param>
    /// <returns>EndpointRouteBuilder</returns>
    public static IEndpointRouteBuilder UseMinimalEndpoints(this IEndpointRouteBuilder builder, Action<EndpointConfiguration>? configuration)
    {
        ServiceProvider = builder.ServiceProvider;

        using var scope = builder.ServiceProvider.CreateScope();
        var services = scope.ServiceProvider;

        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("MinimalEndpoint");

        var endpointDescriptors = builder.ServiceProvider.GetRequiredService<EndpointDescriptors>();

        var definitions = services.GetServices<IEndpointDefinition>();
        var endpoints = services.GetServices<IEndpoint>();

        var serviceConfig = new EndpointConfiguration
        {
            ServiceProvider = builder.ServiceProvider
        };

        configuration?.Invoke(serviceConfig);

        var mappedCount = 0;

        logger.LogTrace("Executing IEndpointDefinition implementations");

        foreach (var definition in definitions)
        {
            var name = definition.GetType().Name;

            try
            {
                definition.MapEndpoint(builder);

                MethodInfo handlerMethodInfo = definition.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                                .FirstOrDefault(m => m.GetCustomAttribute<HandlerMethodAttribute>() != null)!;

                if (handlerMethodInfo != null)
                {
                    var pattern = "";
                    var httpMethod = "";

                    var endpointDataSource = builder.DataSources.Last(e => e.Endpoints.Any(m => m is RouteEndpoint));
                    if (endpointDataSource != null)
                    {
                        var route = (RouteEndpoint)endpointDataSource.Endpoints.Last(); //assume that last entry is the current definition
                        pattern = route.RoutePattern.RawText;
                        httpMethod = route.Metadata.OfType<HttpMethodMetadata>().FirstOrDefault()?.HttpMethods.FirstOrDefault();
                    }
                    var parameterTypes = string.Join(",", handlerMethodInfo.GetParameters()
                            .Select(p => GetParameterTypeName(p.ParameterType)));
                    var handlerMethodName = $"{handlerMethodInfo.DeclaringType!.FullName}.{handlerMethodInfo.Name}({parameterTypes})";

                    endpointDescriptors.Add(new EndpointDescriptor(name, definition.GetType().FullName!, pattern!, httpMethod!, handlerMethodInfo.Name, handlerMethodName, string.Empty));
                }

                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug($"Executing mapping for endpoint definition class '{name}'");
                }

                mappedCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Unhandled error occured while executing endpoint definition for class: {name}");
            }
        }

        logger.LogTrace("Mapping IEndpoint implementations");
        foreach (var endpoint in endpoints)
        {
            if (endpoint is IEndpointDefinition _) continue;
            var name = endpoint.GetType().Name;

            try
            {
                // If the endpoint is also an IEndpointDefinition, prioritize its logic

                var pattern = endpoint.Pattern;

                if (!string.IsNullOrEmpty(serviceConfig.DefaultRoutePrefix))
                    pattern = $"{serviceConfig.DefaultRoutePrefix.TrimEnd('/')}/{pattern.TrimStart('/')}";

                var tagAttr = (EndpointAttribute?)endpoint.GetType().GetTypeInfo().GetCustomAttributes(typeof(EndpointAttribute)).FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(tagAttr?.RoutePrefixOverride))
                {
                    pattern = $"{tagAttr.RoutePrefixOverride.TrimEnd('/')}/{endpoint.Pattern.TrimStart('/')}";
                }

                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug($"Mapping request path '{pattern}' to class '{name}'");
                }

                var methods = new[] { endpoint.Method.Method };

                var routeName = tagAttr?.RouteName ?? string.Empty;

                // Create and add the descriptor to the collection
                var handlerMethodInfo = endpoint.GetType()
                    .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .FirstOrDefault(m => m.IsDefined(typeof(HandlerMethodAttribute), inherit: false))
                    ?? endpoint.Handler.Method;

                //var parameterTypes = string.Join(",", handlerMethodInfo.GetParameters()
                //        .Select(p => GetParameterTypeName(p.ParameterType)));
                //var handlerMethodName = $"{handlerMethodInfo.DeclaringType!.FullName}.{handlerMethodInfo.Name}({parameterTypes})";
                var sb = StringBuilderPool.Get();
                try
                {
                    sb.Append(handlerMethodInfo.DeclaringType?.FullName);
                    sb.Append('.');
                    sb.Append(handlerMethodInfo.Name);
                    sb.Append('(');

                    var parameters = handlerMethodInfo.GetParameters();
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (i > 0) sb.Append(',');
                        sb.Append(GetParameterTypeName(parameters[i].ParameterType));
                    }
                    sb.Append(')');

                    var handlerMethodName = sb.ToString();
                    endpointDescriptors.Add(new EndpointDescriptor(name, endpoint.GetType().FullName!, pattern, endpoint.Method.Method, handlerMethodInfo.Name, handlerMethodName, routeName!));
                }
                finally
                {
                    sb.Clear();
                    StringBuilderPool.Return(sb);
                }

                var (isOverridden, MapEndpoint) = IsMapEndpointOverridden(endpoint.GetType());

                RouteHandlerBuilder mapping = isOverridden ? (RouteHandlerBuilder)MapEndpoint.Invoke(endpoint, [builder])! : builder.MapMethods(pattern, methods, ([FromServices] IServiceProvider sp, [FromServices] ILoggerFactory loggerFactory, HttpRequest request, CancellationToken cancellationToken = default) =>
                {
                    var endpointHandler = sp.GetRequiredService<EndpointHandler>();
                    return endpointHandler.HandleAsync(endpoint, sp, loggerFactory, request, cancellationToken);
                });
                mappedCount++;

                if (!string.IsNullOrWhiteSpace(tagAttr?.RouteName))
                {
                    mapping.WithName(tagAttr.RouteName);
                }

                var globalProduces = serviceConfig.Filters.OfType<ProducesResponseTypeAttribute>();

                var producesRespAttributes = ((ProducesResponseTypeAttribute[])endpoint.GetType().GetTypeInfo().GetCustomAttributes(typeof(ProducesResponseTypeAttribute)))
                        .ToList();

                if (globalProduces.Any()) producesRespAttributes.AddRange(globalProduces);

                foreach (var attr in producesRespAttributes)
                {
                    if (attr.Type == typeof(void))
                    {
                        if (attr.StatusCode == StatusCodes.Status400BadRequest)
                        {
                            mapping.ProducesValidationProblem();
                            continue;
                        }

                        if (attr.StatusCode == StatusCodes.Status500InternalServerError)
                        {
                            mapping.ProducesProblem(attr.StatusCode);
                            continue;
                        }

                        mapping.Produces(attr.StatusCode, responseType: null);
                        continue;
                    }

                    mapping.Produces(attr.StatusCode, responseType: attr.Type);
                }

                foreach (var filter in serviceConfig.EndpointFilters)
                {
                    mapping.AddEndpointFilter(filter);
                }

                if (typeof(EndpointBase).IsAssignableFrom(endpoint.GetType()))
                {
                    var ep = (EndpointBase)endpoint;
                    foreach (var filter in ep.EndpointFilters)
                    {
                        mapping.AddEndpointFilter(filter);
                    }
                }

                var anonAttr = (AllowAnonymousAttribute?)endpoint.GetType().GetTypeInfo().GetCustomAttributes(typeof(AllowAnonymousAttribute)).FirstOrDefault();
                if (anonAttr != null)
                    mapping.AllowAnonymous();
                else
                {
                    var authorizeAttributes = (AuthorizeAttribute[])endpoint.GetType().GetTypeInfo().GetCustomAttributes(typeof(AuthorizeAttribute));
                    foreach (var authData in authorizeAttributes) mapping.RequireAuthorization(authData);
                }

                var corsAttributes = (EnableCorsAttribute[])endpoint.GetType().GetTypeInfo().GetCustomAttributes(typeof(EnableCorsAttribute));
                foreach (var corsAttr in corsAttributes) mapping.RequireCors(corsAttr.PolicyName!);

                var acceptedAttributes = (AcceptAttribute[])endpoint.GetType().GetTypeInfo().GetCustomAttributes(typeof(AcceptAttribute));
                foreach (var acceptAttr in acceptedAttributes)
                {
                    if (acceptAttr.AdditionalContentTypes != null)
                        mapping.Accepts(acceptAttr.Type, acceptAttr.IsOptional, acceptAttr.ContentType, acceptAttr.AdditionalContentTypes);
                    else
                        mapping.Accepts(acceptAttr.Type, acceptAttr.IsOptional, acceptAttr.ContentType);
                }

                if (tagAttr == null) continue;

                if (tagAttr.ExcludeFromDescription) mapping.ExcludeFromDescription();

                if (!string.IsNullOrWhiteSpace(tagAttr.TagName)) mapping.WithTags(tagAttr.TagName);

                if (!string.IsNullOrWhiteSpace(tagAttr.OperationId)) mapping.WithName(tagAttr.OperationId);

                if (!string.IsNullOrWhiteSpace(tagAttr.Description)) mapping.WithDescription(tagAttr.Description);

                if (!string.IsNullOrWhiteSpace(tagAttr.GroupName))
                    mapping.WithGroupName(tagAttr.GroupName);
                else if (serviceConfig.DefaultGroupName is { })
                    mapping.WithGroupName(serviceConfig.DefaultGroupName);

                if (!string.IsNullOrWhiteSpace(serviceConfig.DefaultRateLimitingPolicyName))
                    mapping.RequireRateLimiting(serviceConfig.DefaultRateLimitingPolicyName);

                if (tagAttr.RateLimitingPolicyName is { } || tagAttr.DisableRateLimiting is { })
                {
                    if (!string.IsNullOrWhiteSpace(tagAttr.RateLimitingPolicyName))
                        mapping.RequireRateLimiting(tagAttr.RateLimitingPolicyName);
                    else if (tagAttr.DisableRateLimiting)
                        mapping.DisableRateLimiting();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Unhandled error occured while executing endpoint mapping for class '{name}");
            }
        }

        logger.LogInformation("Total endpoints mapped: {MappedEndpointCount}", mappedCount);

        return builder;
    }

    private static string GetParameterTypeName(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            //Type underlyingType = Nullable.GetUnderlyingType(type)!;
            //return $"System.Nullable{{{underlyingType.FullName?.Replace("+", ".")}}}";

            var underlyingType = Nullable.GetUnderlyingType(type)!;
            var underlyingTypeName = underlyingType.FullName;

            // Avoid unnecessary Replace calls if '+' doesn't exist
            if (underlyingTypeName?.Contains('+') == true)
            {
                underlyingTypeName = underlyingTypeName.Replace("+", ".");
            }

            return $"System.Nullable{{{underlyingTypeName}}}";
        }

        // Handle non-generic types
        //return type.FullName!.Replace("+", ".");

        var fullName = type.FullName;
        if (fullName?.Contains('+') == true)
        {
            fullName = fullName.Replace("+", ".");
        }

        return fullName!;
    }

    private static (bool, MethodInfo) IsMapEndpointOverridden(Type endpointType)
    {
        var method = endpointType.GetMethod(nameof(IEndpoint.MapEndpoint), [typeof(IEndpointRouteBuilder)]);
        return (method != null && method.DeclaringType != typeof(IEndpoint), method!);
    }
}
