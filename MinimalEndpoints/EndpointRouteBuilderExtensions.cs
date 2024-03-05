using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

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

        var endpointDescriptors = builder.ServiceProvider.GetRequiredService<EndpointDescriptors>();

        var endpoints = services.GetServices<IEndpoint>();
        if (endpoints == null) return builder;

        var serviceConfig = new EndpointConfiguration();
        configuration?.Invoke(serviceConfig);

        var endpointHandler = services.GetRequiredService<EndpointHandler>();

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

            var methods = new[] { endpoint.Method.Method };

            // Create and add the descriptor to the collection
            MethodInfo handlerMethodInfo = endpoint.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                                        .FirstOrDefault(m => m.GetCustomAttribute<HandlerMethodAttribute>() != null)!;
            if (handlerMethodInfo == null)
            {
                handlerMethodInfo = endpoint.Handler.Method;
            }

            var parameterTypes = string.Join(",", handlerMethodInfo.GetParameters()
                    .Select(p => p.ParameterType.FullName!.Replace("+", ".")));
            var handlerMethodName = $"{handlerMethodInfo.DeclaringType!.FullName}.{handlerMethodInfo.Name}({parameterTypes})";
            endpointDescriptors.Add(new EndpointDescriptor(endpoint.GetType().FullName!, pattern, endpoint.Method.Method, handlerMethodName));

            var mapping = builder.MapMethods(pattern, methods, ([FromServices] IServiceProvider sp, [FromServices] ILoggerFactory loggerFactory, HttpRequest request, CancellationToken cancellationToken = default) => endpointHandler.HandleAsync(endpoint, sp, loggerFactory, request, cancellationToken));

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

            if (!string.IsNullOrWhiteSpace(tagAttr.TagName)) mapping.WithTags(tagAttr.TagName);

            if (!string.IsNullOrWhiteSpace(tagAttr.OperationId)) mapping.WithName(tagAttr.OperationId);

            if (!string.IsNullOrWhiteSpace(tagAttr.GroupName))
                mapping.WithGroupName(tagAttr.GroupName);
            else if (serviceConfig.DefaultGroupName is { })
                mapping.WithGroupName(serviceConfig.DefaultGroupName);

            if (!string.IsNullOrWhiteSpace(tagAttr.Description)) mapping.WithDescription(tagAttr.Description);

            if (!string.IsNullOrWhiteSpace(tagAttr.RateLimitingPolicyName)) mapping.RequireRateLimiting(tagAttr.RateLimitingPolicyName);
        }

        return builder;
    }
}
