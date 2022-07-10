using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MinimalEndpoints;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder UseMinimalEndpoints(this IEndpointRouteBuilder builder)
    {
        var app = builder.CreateApplicationBuilder();

        var endpoints = app.ApplicationServices.GetServices<IEndpoint>();
        if (endpoints == null) return builder;

        foreach (var endpoint in endpoints)
        {
            var mapping = builder.MapMethods(endpoint.Pattern, new[] { endpoint.Method.Method }, endpoint.Handler);

            var producesRespAttributes = (ProducesResponseTypeAttribute[])endpoint.GetType().GetTypeInfo().GetCustomAttributes(typeof(ProducesResponseTypeAttribute));
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
            if (anonAttr != null) mapping.AllowAnonymous();

            var authorizeAttributes = (AuthorizeAttribute[])endpoint.GetType().GetTypeInfo().GetCustomAttributes(typeof(AuthorizeAttribute));
            foreach (var authData in authorizeAttributes) mapping.RequireAuthorization(authData);

            var corsAttributes = (EnableCorsAttribute[])endpoint.GetType().GetTypeInfo().GetCustomAttributes(typeof(EnableCorsAttribute));
            foreach (var corsAttr in corsAttributes) mapping.RequireCors(corsAttr.PolicyName);

            var acceptedAttributes = (AcceptAttribute[])endpoint.GetType().GetTypeInfo().GetCustomAttributes(typeof(AcceptAttribute));
            foreach (var acceptAttr in acceptedAttributes)
            {
                if (acceptAttr.AdditionalContentTypes != null)
                    mapping.Accepts(acceptAttr.Type, acceptAttr.IsOptional, acceptAttr.ContentType, acceptAttr.AdditionalContentTypes);
                else
                    mapping.Accepts(acceptAttr.Type, acceptAttr.IsOptional, acceptAttr.ContentType);
            }

            var tagAttr = (EndpointAttribute?)endpoint.GetType().GetTypeInfo().GetCustomAttributes(typeof(EndpointAttribute)).FirstOrDefault();
            if (tagAttr == null) continue;

            if (tagAttr.ExcludeFromDescription) mapping.ExcludeFromDescription();

            if (!string.IsNullOrEmpty(tagAttr.TagName)) mapping.WithTags(tagAttr.TagName);

            if (!string.IsNullOrEmpty(tagAttr.OperatinId)) mapping.WithName(tagAttr.OperatinId);
        }

        return builder;
    }
}
