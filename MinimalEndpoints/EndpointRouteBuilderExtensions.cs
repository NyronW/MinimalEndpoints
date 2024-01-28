using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MinimalEndpoints.Extensions.Http;
using System.Reflection;
using System.Text.Json;
using System.Xml.Serialization;

namespace MinimalEndpoints;

public static class EndpointRouteBuilderExtensions
{
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
        using var scope = builder.ServiceProvider.CreateScope();
        var services = scope.ServiceProvider;

        var endpoints = services.GetServices<IEndpoint>();
        if (endpoints == null) return builder;

        var serviceConfig = new EndpointConfiguration();
        configuration?.Invoke(serviceConfig);

        foreach (var endpoint in endpoints)
        {
            var pattern = endpoint.Pattern;

            if (!string.IsNullOrEmpty(serviceConfig.DefaultRoutePrefix))
                pattern = $"{serviceConfig.DefaultRoutePrefix.TrimEnd('/')}/{pattern.TrimStart('/')}";

            var tagAttr = (EndpointAttribute?)endpoint.GetType().GetTypeInfo().GetCustomAttributes(typeof(EndpointAttribute)).FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(tagAttr?.RoutePrefixOverride))
            {
                pattern = $"{tagAttr.RoutePrefixOverride.TrimEnd('/')}/{endpoint.Pattern.TrimStart('/')}";
            }

            var handler = async (IServiceProvider sp, HttpRequest request) =>
            {
                object result = null!;

                try
                {
                    var ep = (IEndpoint)sp.GetService(endpoint.GetType())!;

                    MethodInfo methodInfo = ep.Handler.Method;
                    ParameterInfo[] parameters = methodInfo.GetParameters();

                    object[] args = new object[parameters.Length];

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var param = parameters[i];
                        if (param is not { Name.Length: > 0 }) continue;

                        object value = null!;
                        object requestBodyObject = null!;

                        string stringValue = request.RouteValues[param.Name]?.ToString()! ??
                                             request.Query[param.Name].FirstOrDefault()! ??
                                             request.Headers[param.Name].FirstOrDefault()!;

                        if (param.ParameterType == typeof(HttpContext))
                        {
                            value = request.HttpContext;
                        }
                        else if (param.ParameterType == typeof(HttpRequest))
                        {
                            value = request;
                        }
                        else if (!string.IsNullOrEmpty(stringValue))
                        {
                            value = ConvertParameter(stringValue, param.ParameterType);
                        }
                        else if (param.ParameterType.IsValueType && Nullable.GetUnderlyingType(param.ParameterType) == null)
                        {
                            if (param.HasDefaultValue)
                            {
                                value = param.DefaultValue!;
                            }
                            else
                            {
                                throw new InvalidOperationException($"The value for parameter '{param.Name}' was not found in the request and does not have a default value.");
                            }
                        }

                        if (value == null && (!param.ParameterType.IsValueType || Nullable.GetUnderlyingType(param.ParameterType) != null))
                        {
                            if (requestBodyObject == null && request.ContentLength > 0)
                            {
                                if (request.ContentType == "application/json")
                                {
                                    requestBodyObject = (await request.ReadFromJsonAsync(param.ParameterType))!;
                                }
                                else if (request.ContentType == "application/xml")
                                {
                                    requestBodyObject = (await request.ReadFromXmlAsync(param.ParameterType))!;
                                }
                            }
                            value = requestBodyObject ?? (param.HasDefaultValue ? param.DefaultValue : null!)!;
                        }

                        args[i] = value!;
                    }

                    // Invoke the delegate with the dynamically bound parameters
                    bool isAsync = typeof(Task).IsAssignableFrom(methodInfo.ReturnType);
                    bool isGenericTask = methodInfo.ReturnType.IsGenericType && methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);

                    if (isAsync)
                    {
                        var task = (Task)methodInfo.Invoke(ep.Handler.Target, args)!;
                        await task.ConfigureAwait(false);
                        result = isGenericTask ? ((dynamic)task).Result : null!;
                    }
                    else
                    {
                        // Invoke the delegate synchronously
                        result = methodInfo.Invoke(ep.Handler.Target, args)!;
                    }
                }
                catch
                {
                    throw;
                }

                return result;
            };

            var mapping = builder.MapMethods(pattern, new[] { endpoint.Method.Method }, handler);

            var globalProduces = serviceConfig.Filters.Where(f => f is ProducesResponseTypeAttribute)
                .Cast<ProducesResponseTypeAttribute>();

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

            if (!string.IsNullOrEmpty(tagAttr.TagName)) mapping.WithTags(tagAttr.TagName);

            if (!string.IsNullOrEmpty(tagAttr.OperationId)) mapping.WithName(tagAttr.OperationId);

            if (!string.IsNullOrEmpty(tagAttr.GroupName))
                mapping.WithGroupName(tagAttr.GroupName);
            else if (serviceConfig.DefaultGroupName is { })
                mapping.WithGroupName(serviceConfig.DefaultGroupName);
        }

        return builder;
    }

    private static object ConvertParameter(string value, Type type)
    {
        return string.IsNullOrEmpty(value)! ?
               (type.IsValueType ? Activator.CreateInstance(type) : null!)! :
               Convert.ChangeType(value, type)!;
    }
}
